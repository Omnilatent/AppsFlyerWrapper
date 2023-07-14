using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AppsFlyerSDK;
using UnityEngine;
#if UNITY_IOS
using Unity.Notifications.iOS;
#endif
using Firebase.Messaging;
using Firebase.Unity;

namespace Omnilatent.AppsFlyerWrapperNS
{
    public class UninstallMeasurement
    {
        public static void Init()
        {
            ListenToFCMEvent();
#if UNITY_IOS
        RequestAuthorization();

        async void RequestAuthorization()
        {
            using (var req = new AuthorizationRequest(AuthorizationOption.Alert | AuthorizationOption.Badge, true))
            {
                while (!req.IsFinished) { await Task.Delay(20); }

                if (req.Granted && req.DeviceToken != "") { AppsFlyer.registerUninstall(Encoding.UTF8.GetBytes(req.DeviceToken)); }
            }
        }
#endif
        }

        private static async void ListenToFCMEvent()
        {
            await Task.Delay(500);
            Firebase.Messaging.FirebaseMessaging.TokenReceived += OnTokenReceived;
        }

        public static void OnTokenReceived(object sender, Firebase.Messaging.TokenReceivedEventArgs token)
        {
#if UNITY_ANDROID
            AppsFlyer.updateServerUninstallToken(token.Token);
#endif
        }
    }
}