using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
#if OMNILATENT_APPSFLYER_WRAPPER
using AppsFlyerConnector;
#endif
using AppsFlyerSDK;
using Firebase.Analytics;
using UnityEngine;

namespace Omnilatent.AppsFlyerWrapperNS
{
    public class AppsFlyerWrapper : MonoBehaviour, IAppsFlyerConversionData
    {
        public bool initializeAutomatically = true;
        public string devKey;
        public string appID;
        public bool getConversionData;
        public bool debugLogEvent = false;
        public string eventLogOnAppLoseFocus = "in_background";
        public static bool logAdRevenueAsEvent = true; //log an event alongside appsflyer' ad revenue API
        private const string REVENUE_PARAM_NAME = "revenue";
        public static Action<object, AppsFlyerRequestEventArgs> OnRequestResponse;
        private static bool initialized = false;

        public static bool Initialized
        {
            get => initialized;
        }

        private static AppsFlyerWrapper instance;

        public static AppsFlyerWrapper Instance
        {
            get { return instance; }
        }

        private void Start()
        {
            if (instance == null) { instance = this; }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            if (initializeAutomatically) Initialize();
        }

        public void Initialize()
        {
            bool isDebug = Debug.isDebugBuild;
            AppsFlyerSDK.AppsFlyer.setIsDebug(isDebug);
            AppsFlyerSDK.AppsFlyer.initSDK(devKey, appID, getConversionData ? this : null);

#if OMNILATENT_APPSFLYER_WRAPPER
            AppsFlyerPurchaseConnector.init(this, AppsFlyerConnector.Store.GOOGLE);
            AppsFlyerPurchaseConnector.setIsSandbox(isDebug);
            AppsFlyerPurchaseConnector.setAutoLogPurchaseRevenue(
                AppsFlyerAutoLogPurchaseRevenueOptions.AppsFlyerAutoLogPurchaseRevenueOptionsAutoRenewableSubscriptions,
                AppsFlyerAutoLogPurchaseRevenueOptions.AppsFlyerAutoLogPurchaseRevenueOptionsInAppPurchases);
            AppsFlyerPurchaseConnector.setPurchaseRevenueValidationListeners(true);
            AppsFlyerPurchaseConnector.build();
            AppsFlyerPurchaseConnector.startObservingTransactions();

            AppsFlyerAdRevenue.setIsDebug(isDebug);
            AppsFlyerAdRevenue.start();
#endif
            AppsFlyerSDK.AppsFlyer.startSDK();
            UninstallMeasurement.Init();
            initialized = true;
        }

        // Mark AppsFlyer CallBacks
        public void onConversionDataSuccess(string conversionData)
        {
            AppsFlyerSDK.AppsFlyer.AFLog("didReceiveConversionData", conversionData);
            Dictionary<string, object> conversionDataDictionary = AppsFlyerSDK.AppsFlyer.CallbackStringToDictionary(conversionData);
            // add deferred deeplink logic here
        }

        public void onConversionDataFail(string error) { AppsFlyerSDK.AppsFlyer.AFLog("didReceiveConversionDataWithError", error); }

        public void onAppOpenAttribution(string attributionData)
        {
            AppsFlyerSDK.AppsFlyer.AFLog("onAppOpenAttribution", attributionData);
            Dictionary<string, object> attributionDataDictionary = AppsFlyerSDK.AppsFlyer.CallbackStringToDictionary(attributionData);
            // add direct deeplink logic here
        }

        public void onAppOpenAttributionFailure(string error) { AppsFlyerSDK.AppsFlyer.AFLog("onAppOpenAttributionFailure", error); }

        public static void LogEvent(string name, string paramName, int value) { LogEvent(name, paramName, value.ToString()); }

        public static void LogEvent(string name, string paramName, double value) { LogEvent(name, paramName, value.ToString()); }

        public static void LogEvent(string name, string paramName, string value)
        {
            if (!Initialized) { return; }

            CheckEventNameValid(name);
            LogConsole(name, paramName, value);
            Dictionary<string, string> eventValues = new Dictionary<string, string>();
            eventValues.Add(paramName, value);
            AppsFlyerSDK.AppsFlyer.sendEvent(name, eventValues);
        }

        public static void LogEvent(string name, Dictionary<string, string> value)
        {
            if (!Initialized) { return; }

            CheckEventNameValid(name);
            AppsFlyerSDK.AppsFlyer.sendEvent(name, value);
        }

        public static void LogEvent(string name) { LogEvent(name, string.Empty, String.Empty); }

        public static void TrackRevenueAdmob(double value, string currencyCode, string eventName = "", Dictionary<string, string> additionalData = null)
        {
#if OMNILATENT_APPSFLYER_WRAPPER
            value = value / 1000000;
            string valueStr = value.ToString("0.0000000", System.Globalization.CultureInfo.InvariantCulture);
            System.Collections.Generic.Dictionary<string, string> adRevenueEvent = new System.Collections.Generic.Dictionary<string, string>();
            // adRevenueEvent.Add(AFInAppEvents.CURRENCY, currencyCode);
            // adRevenueEvent.Add(AFInAppEvents.REVENUE, value.ToString());
            adRevenueEvent.Add(AFInAppEvents.QUANTITY, "1");
            adRevenueEvent.Add(AFInAppEvents.CONTENT_TYPE, "admob_revenue");

            if (additionalData != null)
            {
                foreach (var data in additionalData) { adRevenueEvent.Add(data.Key, data.Value); }
            }

            eventName = string.IsNullOrEmpty(eventName) ? "show_ad" : eventName;
            // AppsFlyer.sendEvent(eventName, adRevenueEvent);
            AppsFlyerAdRevenue.logAdRevenue("admob", AppsFlyerAdRevenueMediationNetworkType.AppsFlyerAdRevenueMediationNetworkTypeGoogleAdMob, value,
                currencyCode, adRevenueEvent);
            if (logAdRevenueAsEvent)
            {
                adRevenueEvent.Add(AFInAppEvents.CURRENCY, currencyCode);
                adRevenueEvent.Add(REVENUE_PARAM_NAME, valueStr);
                LogEvent(eventName, adRevenueEvent);
            }

            Debug.Log($"AppsFlyer tracked {valueStr} {currencyCode}");
#endif
        }

        public static void TrackRevenueMAX(double value, string currencyCode, string eventName = "", Dictionary<string, string> additionalData = null)
        {
#if OMNILATENT_APPSFLYER_WRAPPER
            string valueStr = value.ToString("0.0000000", System.Globalization.CultureInfo.InvariantCulture);
            System.Collections.Generic.Dictionary<string, string> adRevenueEvent = new System.Collections.Generic.Dictionary<string, string>();
            // adRevenueEvent.Add(AFInAppEvents.CURRENCY, currencyCode);
            // adRevenueEvent.Add(AFInAppEvents.REVENUE, value.ToString());
            adRevenueEvent.Add(AFInAppEvents.QUANTITY, "1");
            adRevenueEvent.Add(AFInAppEvents.CONTENT_TYPE, "max_revenue");

            if (additionalData != null)
            {
                foreach (var data in additionalData) { adRevenueEvent.Add(data.Key, data.Value); }
            }

            eventName = string.IsNullOrEmpty(eventName) ? "show_ad" : eventName;
            // AppsFlyer.sendEvent(eventName, adRevenueEvent);
            AppsFlyerAdRevenue.logAdRevenue("max", AppsFlyerAdRevenueMediationNetworkType.AppsFlyerAdRevenueMediationNetworkTypeApplovinMax, value,
                currencyCode, adRevenueEvent);
            if (logAdRevenueAsEvent)
            {
                adRevenueEvent.Add(AFInAppEvents.CURRENCY, currencyCode);
                adRevenueEvent.Add(REVENUE_PARAM_NAME, valueStr);
                LogEvent(eventName, adRevenueEvent);
            }

            Debug.Log($"AppsFlyer tracked {value} {currencyCode}");
#endif
        }

        static bool CheckEventNameValid(string eventName, string paramName = "")
        {
            bool isDebugging = Debug.isDebugBuild;
            bool isValid = true;
#if UNITY_EDITOR
            isDebugging = true;
#endif
            if (isDebugging)
            {
                string regexPattern = @"^[a-zA-Z]\w+$";
                if (eventName.Length > 40 || paramName.Length > 40)
                {
                    var e = new System.ArgumentException($"Event '{eventName}' with param '{paramName}' exceeds 40 characters");
                    Debug.LogException(e);
                    isValid = false;
                }

                if (!Regex.Match(eventName, regexPattern).Success || (!string.IsNullOrEmpty(paramName) && !Regex.Match(paramName, regexPattern).Success))
                {
                    var e = new System.ArgumentException($"Event '{eventName}' with param '{paramName}' contains invalid characters");
                    Debug.LogException(e);
                    isValid = false;
                }
            }

            return isValid;
        }

        static void LogConsole(string name, string paramName = "", object value = null)
        {
#if UNITY_EDITOR
            if (Instance.debugLogEvent) { Debug.Log($"<color=yellow>AppsFlyer log:</color> {name}, {paramName}, {value}"); }
#endif
        }

        public void didReceivePurchaseRevenueValidationInfo(string validationInfo)
        {
            AppsFlyer.AFLog("didReceivePurchaseRevenueValidationInfo", validationInfo);
        }

        void AppsFlyerOnRequestResponse(object sender, EventArgs e)
        {
            var args = e as AppsFlyerRequestEventArgs;
            AppsFlyer.AFLog("AppsFlyerOnRequestResponse", " status code " + args.statusCode);
            Debug.Log("AppsFlyerOnRequestResponse " + args.statusCode);
            OnRequestResponse?.Invoke(sender, args);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && !string.IsNullOrEmpty(eventLogOnAppLoseFocus)) { LogEvent(eventLogOnAppLoseFocus); }
        }
    }
}