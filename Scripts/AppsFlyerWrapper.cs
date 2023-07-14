using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AppsFlyerConnector;
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

        private static AppsFlyerWrapper instance;

        public static AppsFlyerWrapper Instance
        {
            get { return instance; }
        }

        private void Start()
        {
            if (initializeAutomatically) Init();
        }

        public void Init()
        {
            if (instance == null) { instance = this; }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            bool isDebug = Debug.isDebugBuild;
            AppsFlyerSDK.AppsFlyer.setIsDebug(isDebug);
            AppsFlyerSDK.AppsFlyer.initSDK(devKey, appID, getConversionData ? this : null);

            AppsFlyerPurchaseConnector.init(this, AppsFlyerConnector.Store.GOOGLE);
            AppsFlyerPurchaseConnector.setIsSandbox(isDebug);
            AppsFlyerPurchaseConnector.setAutoLogPurchaseRevenue(
                AppsFlyerAutoLogPurchaseRevenueOptions.AppsFlyerAutoLogPurchaseRevenueOptionsAutoRenewableSubscriptions,
                AppsFlyerAutoLogPurchaseRevenueOptions.AppsFlyerAutoLogPurchaseRevenueOptionsInAppPurchases);
            AppsFlyerPurchaseConnector.setPurchaseRevenueValidationListeners(true);
            AppsFlyerPurchaseConnector.build();
            AppsFlyerPurchaseConnector.startObservingTransactions();

            AppsFlyerSDK.AppsFlyer.startSDK();

            UninstallMeasurement.Init();
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
            CheckEventNameValid(name);
            LogConsole(name, paramName, value);
            Dictionary<string, string> eventValues = new Dictionary<string, string>();
            eventValues.Add(paramName, value);
            AppsFlyerSDK.AppsFlyer.sendEvent(name, eventValues);
        }

        public static void LogEvent(string name, Dictionary<string, string> value)
        {
            CheckEventNameValid(name);
            AppsFlyerSDK.AppsFlyer.sendEvent(name, value);
        }

        public static void LogEvent(string name) { LogEvent(name, string.Empty, String.Empty); }

        public static void TrackRevenueAdmob(int value, string currencyCode, string eventName = "", Dictionary<string, string> additionalData = null)
        {
            System.Collections.Generic.Dictionary<string, string> adRevenueEvent = new System.Collections.Generic.Dictionary<string, string>();
            adRevenueEvent.Add(AFInAppEvents.CURRENCY, currencyCode);
            adRevenueEvent.Add(AFInAppEvents.REVENUE, value.ToString());
            // adRevenueEvent.Add(AFInAppEvents.QUANTITY, "1");
            adRevenueEvent.Add(AFInAppEvents.CONTENT_TYPE, "admob_revenue");

            if (additionalData != null)
            {
                foreach (var data in additionalData) { adRevenueEvent.Add(data.Key, data.Value); }
            }

            eventName = string.IsNullOrEmpty(eventName) ? "af_show_ad_interstitial" : eventName;
            AppsFlyer.sendEvent(eventName, adRevenueEvent);
            Debug.Log($"AppsFlyer tracked {value} {currencyCode}");
        }
        
        public static void TrackRevenueMAX(double value, string currencyCode, string eventName = "", Dictionary<string, string> additionalData = null)
        {
            System.Collections.Generic.Dictionary<string, string> adRevenueEvent = new System.Collections.Generic.Dictionary<string, string>();
            adRevenueEvent.Add(AFInAppEvents.CURRENCY, currencyCode);
            adRevenueEvent.Add(AFInAppEvents.REVENUE, value.ToString());
            // adRevenueEvent.Add(AFInAppEvents.QUANTITY, "1");
            adRevenueEvent.Add(AFInAppEvents.CONTENT_TYPE, "max_revenue");

            if (additionalData != null)
            {
                foreach (var data in additionalData) { adRevenueEvent.Add(data.Key, data.Value); }
            }

            eventName = string.IsNullOrEmpty(eventName) ? "af_show_ad_interstitial" : eventName;
            AppsFlyer.sendEvent(eventName, adRevenueEvent);
            Debug.Log($"AppsFlyer tracked {value} {currencyCode}");
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
    }
}