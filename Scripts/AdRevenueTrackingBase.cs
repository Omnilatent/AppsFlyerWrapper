using System.Collections.Generic;
using UnityEngine;

namespace Omnilatent.AppsFlyerWrapperNS
{
    public abstract class AdRevenueTrackingBase : MonoBehaviour
    {
        public abstract void TrackRevenueAdmob(double value, string currencyCode, Dictionary<string, string> additionalData = null);

        public abstract void TrackRevenueMAX(double value, string currencyCode, Dictionary<string, string> additionalData = null);
    }
}