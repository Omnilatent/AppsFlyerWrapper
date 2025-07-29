using System.Collections.Generic;
using AppsFlyerSDK;
using UnityEngine;

namespace Omnilatent.AppsFlyerWrapperNS
{
    public class AdRevenueTrackingForTiktok : AdRevenueTrackingBase
    {
        [SerializeField] private string _eventName = "af_ad_revenue2";

        private const string monetization_network = "monetization_network";
        private const string mediation_network = "mediation_network";
        private const string event_revenue_currency = "event_revenue_currency";
        private const string event_revenue = "event_revenue";

        private const string placement = "placement";
        private const string googleadmob = "googleadmob";
        private const string applovinmax = "applovinmax";

        /*
         monetization_network: applovinmax
        mediation_network: applovinmax
        event_revenue_currency: USD
        event_revenue: 0.001257291
        placement: 401
        custom_parameters: 
        {"ad_format":"OpenAd","ad_unit_id":"cd5885d05d7660be","precision_type":"exact","revenue_source":"Google AdMob"}
         */
        public override void TrackRevenueAdmob(double value, string currencyCode, Dictionary<string, string> additionalData = null)
        {
            string valueStr = AppsFlyerWrapper.RevenueToString(value);

            Dictionary<string, string> adRevenueParams = new Dictionary<string, string>();
            adRevenueParams.Add(monetization_network, googleadmob);
            adRevenueParams.Add(mediation_network, googleadmob);
            adRevenueParams.Add(event_revenue_currency, currencyCode);
            adRevenueParams.Add(event_revenue, valueStr);

            if (additionalData != null)
            {
                foreach (var data in additionalData)
                {
                    if (!adRevenueParams.TryAdd(data.Key, data.Value))
                    {
                        Debug.LogError($"adRevenueParams: key already exist: {data.Key}");
                    }
                }
            }

            AppsFlyerWrapper.LogEvent(_eventName, adRevenueParams);
            Debug.Log($"AppsFlyer tracked admob 2 {value} {currencyCode}");
        }

        public override void TrackRevenueMAX(double value, string currencyCode, Dictionary<string, string> additionalData = null)
        {
            string valueStr = AppsFlyerWrapper.RevenueToString(value);

            Dictionary<string, string> adRevenueParams = new Dictionary<string, string>();
            adRevenueParams.Add(monetization_network, applovinmax);
            adRevenueParams.Add(mediation_network, applovinmax);
            adRevenueParams.Add(event_revenue_currency, currencyCode);
            adRevenueParams.Add(event_revenue, valueStr);
            /*if (additionalData.TryGetValue(placement, out string placementValue))
            {
                adRevenueParams.Add(placement, placementValue);
            }*/

            if (additionalData != null)
            {
                foreach (var data in additionalData)
                {
                    if (!adRevenueParams.TryAdd(data.Key, data.Value))
                    {
                        Debug.LogError($"adRevenueParams: key already exist: {data.Key}");
                    }
                }
            }

            AppsFlyerWrapper.LogEvent(_eventName, adRevenueParams);
            Debug.Log($"AppsFlyer tracked max 2 {value} {currencyCode}");
        }
    }
}