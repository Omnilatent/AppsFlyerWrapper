using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

            AppsFlyerSDK.AppsFlyer.setIsDebug(Debug.isDebugBuild);
            AppsFlyerSDK.AppsFlyer.initSDK(devKey, appID, getConversionData ? this : null);
            AppsFlyerSDK.AppsFlyer.startSDK();
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

        /* setup on dashboard for revenue
        public static void TrackRevenueAdmob(int value, string currencyCode)
        {
            System.Collections.Generic.Dictionary<string, string> adRevenueEvent = new System.Collections.Generic.Dictionary<string, string>();
            adRevenueEvent.Add(AFInAppEvents.CURRENCY, currencyCode);
            adRevenueEvent.Add(AFInAppEvents.REVENUE, value.ToString());
            adRevenueEvent.Add(AFInAppEvents.QUANTITY, "1");
            adRevenueEvent.Add(AFInAppEvents.CONTENT_TYPE, "admob_revenue");
            AppsFlyer.sendEvent(AFInAppEvents.PURCHASE, adRevenueEvent);
            Debug.Log($"AppsFlyer tracked");
        }*/

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
    }
}