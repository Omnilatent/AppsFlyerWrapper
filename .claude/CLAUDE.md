# AppsFlyer Wrapper

Thin wrapper around the AppsFlyer Unity SDK (Omnilatent package `com.omnilatent.appsflyerwrapper`,
v1.1.0). Handles SDK init, event logging, ad-revenue tracking (AdMob / MAX), the purchase connector,
uninstall measurement, and the iOS SKAdNetwork endpoint. It adds no ad-loading or attribution logic
of its own — everything is a pass-through to `AppsFlyerSDK.*`.

## Identity quirks (read before grep-ing)

- **Assembly name ≠ file name.** The asmdef file is `Omnilatent.AppsFlyerWrapper.asmdef` but the
  assembly name inside is `Omnilatent.AppsFlyerUtils`. Editor asmdef is
  `Omnilatent.AppsFlyerWrapper.EditorNS`.
- **Namespace** for all runtime + editor code is `Omnilatent.AppsFlyerWrapperNS`
  (editor: `Omnilatent.AppsFlyerWrapperNS.EditorNS`). Note the `NS` suffix — it does not match the
  package/asmdef name.

## Dependencies (external, not vendored here)

- `appsflyer-unity-plugin` 6.10.x+ — the SDK. asmdef refs: `AppsFlyer`, `AppsFlyerConnector`,
  `AppsFlyerAdRevenueConnector`, `AppsFlyerAdRevenue`.
- `appsflyer-unity-purchase-connector` 1.0.x+ — IAP revenue (gated, see below).
- Firebase (Cloud Messaging for uninstall, Analytics referenced in `AppsFlyerWrapper.cs`).
  asmdef ref: `Omnilatent.Firebase`.
- Unity Mobile Notifications (`Unity.Notifications.iOS`) — iOS uninstall auth.

As of AppsFlyer 6.15.0 the Ad Revenue connector is folded into the SDK (CHANGELOG 1.1.0).

## Setup (one-time, per project)

Menu **Tools → Omnilatent → AppsFlyer → Import essential files** (`InitialSetup` EditorWindow):

1. **Initialize Scripting Define Symbol** → adds `OMNILATENT_APPSFLYER_WRAPPER`. **This gates all
   revenue tracking and the purchase connector** — without it `TrackRevenueAdmob`/`TrackRevenueMAX`
   compile to empty no-ops that fail silently. This is the #1 "revenue not tracked" cause.
2. (Optional) Import `AppsFlyerConnectorAsmdef.unitypackage` — the window comment says this is no
   longer required as of AppsFlyer 6.14.3.

Then place the `AppsFlyer Wrapper.prefab` in the first scene, set **Dev Key** (+ **App ID** for iOS).

## Runtime flow

`AppsFlyerWrapper : MonoBehaviour, IAppsFlyerConversionData, IAppsFlyerPurchaseRevenueDataSource`
— static singleton (`Instance`), `DontDestroyOnLoad`.

`Start()` → singleton guard → `Initialize()` if `initializeAutomatically`.

`Initialize()` order (matters):
1. `setIsDebug(Debug.isDebugBuild)`, validate `devKey` (+ `appID` on iOS) — logs an exception, does
   **not** abort if empty.
2. `initSDK(devKey, appID, getConversionData ? this : null)`.
3. `[OMNILATENT_APPSFLYER_WRAPPER]` Purchase Connector: `init(this, Store.GOOGLE)` →
   `ConfigurePurchaseConnector()` (sandbox = isDebug, auto-log subs + IAP, validation listeners,
   iOS data source) → `build()` → `startObservingTransactions()`. Wrapped in try/catch — a missing
   Unity IAP package is swallowed with a log, not fatal.
4. iOS: `waitForATTUserAuthorizationWithTimeoutInterval(60)`.
5. `startSDK()`.
6. `UninstallMeasurement.Init()`.
7. Collect sibling `AdRevenueTrackingBase` components into `_revenueTrackers`.
8. `initialized = true` — `LogEvent`/`TrackRevenue*` are guarded on this and return early before it.

## Event logging

`LogEvent(name, ...)` overloads → `AppsFlyer.sendEvent`. Guarded by `Initialized`.
`CheckEventNameValid` runs **only in debug builds / editor**: regex `^[a-zA-Z]\w+$`, max 40 chars for
both event and param name. It logs an exception but does **not** block the send — validation is a
dev-time warning, not an enforced gate.

`OnApplicationFocus(false)` logs `eventLogOnAppLoseFocus` (default `"in_background"`).

## Ad-revenue tracking

Called from the game's Ad-Paid callback (e.g. AdsManager's `HandleAdmobMessage` /
`HandleMAXMessage`). Both are `static`, both gated by `OMNILATENT_APPSFLYER_WRAPPER`.

`TrackRevenueAdmob(value, currencyCode, eventName="", additionalData=null)`:
- **Divides `value` by 1,000,000** (AdMob reports micros → dollars). Caller passes the raw AdMob
  value, un-divided.
- `TrackRevenueMAX` does **not** divide (MAX already reports dollars).
- Both call `AppsFlyer.logAdRevenue(new AFAdRevenueData(<network>, MediationNetwork.*, "USD", value), additionalData)`.
- If `logAdRevenueAsEvent` (static, default true) also sends a `show_ad` (default) in-app event with
  currency + revenue params.
- Forwards to every registered `AdRevenueTrackingBase` (see extension point).

**Gotcha — currency is hardcoded `"USD"`** in the `AFAdRevenueData` constructor. The `currencyCode`
parameter only reaches the optional event log, **not** the actual AppsFlyer ad-revenue API. For
non-USD revenue the reported ad-revenue currency will be wrong. Unverified whether this is
intentional (most mediation revenue is normalized to USD) or a latent bug — flag before relying on
multi-currency reporting.

### Extension point: `AdRevenueTrackingBase`

Abstract `MonoBehaviour` with `TrackRevenueAdmob` / `TrackRevenueMAX`. Attach any subclass to the
same GameObject as the wrapper; `Initialize()` auto-collects them and the static `TrackRevenue*`
fans out to each. Lets a project add extra attribution sinks without touching the wrapper.

`AdRevenueTrackingForTiktok` — the shipped concrete tracker. Logs a separate `af_ad_revenue2` event
(TikTok's expected schema: `monetization_network`, `mediation_network`, `event_revenue_currency`,
`event_revenue`) so TikTok attribution can read ad revenue alongside AppsFlyer's native API.

## Uninstall measurement (`UninstallMeasurement`)

FCM device token → uninstall registration.
- Android: `TokenReceived` → `AppsFlyer.updateServerUninstallToken(token)`.
- iOS: notification `AuthorizationRequest`, then `AppsFlyer.registerUninstall(deviceToken)`.
- `[OMNILATENT_FIREBASE_MANAGER]` gates whether FCM subscription waits on `FirebaseManager` readiness
  vs. subscribing immediately.

## Editor build hook

`AppsflyerEndpointRegister.OnPostprocessBuild` (iOS only) writes
`NSAdvertisingAttributionReportEndpoint = https://appsflyer-skadnetwork.com/` into `Info.plist` for
SKAdNetwork aggregated reporting.

## Conditional compilation

- `OMNILATENT_APPSFLYER_WRAPPER` — gates **all** revenue tracking + purchase connector. Set via the
  setup window. Absent → those methods are empty no-ops.
- `OMNILATENT_FIREBASE_MANAGER` — routes FCM subscription through `FirebaseManager` readiness.
- `UNITY_IOS` / `UNITY_ANDROID` — platform-specific uninstall + ATT + plist paths.

## Known gotchas

- **Coroutine soft-lock (README):** AppsFlyer + Firebase have blocked coroutines in the first scene,
  soft-locking the game. If the first scene stalls, convert its coroutines to C# Task / UniTask.
- **Silent no-op without the define symbol** (see Setup #1) — the most common integration failure.
- **Hardcoded `"USD"`** in ad-revenue reporting (see Ad-revenue tracking).
- **Empty dev key / app ID do not abort init** — only log an exception; the SDK still starts with
  bad config.

## File map

```
Scripts/
  AppsFlyerWrapper.cs          # singleton, init, LogEvent, TrackRevenue*, purchase connector
  AdRevenueTrackingBase.cs     # abstract extra-tracker extension point
  AdRevenueTrackingForTiktok.cs# concrete tracker → af_ad_revenue2 event
  UninstallMeasurement.cs      # FCM token → uninstall registration
Editor/
  InitialSetup.cs              # setup window: define symbol + connector import
  AppsflyerEndpointRegister.cs # iOS post-build: SKAdNetwork plist endpoint
AppsFlyer Wrapper.prefab       # drop into first scene, set Dev Key / App ID
```
