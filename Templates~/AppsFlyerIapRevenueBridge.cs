using System;
using System.Collections.Generic;
using System.Globalization;
using AppsFlyerSDK;
using Omnilatent.InAppPurchase;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Omnilatent.AppsFlyerWrapperNS
{
    /// <summary>
    /// Bắn doanh thu IAP lên AppsFlyer từ phía game, dùng cho project mà AppsFlyer Purchase
    /// Connector không tự log revenue.
    ///
    /// Mặc định TẮT (<see cref="Mode.Disabled"/>). Purchase Connector chạy đúng mà bật thêm
    /// component này thì doanh thu bị đếm hai lần — chỉ bật khi đã xác nhận connector im lặng
    /// bằng log device.
    ///
    /// Component phải nằm trên GameObject sống xuyên scene: callback validate từ native được
    /// route ngược về theo tên GameObject.
    /// </summary>
    [DisallowMultipleComponent]
    public class AppsFlyerIapRevenueBridge : MonoBehaviour, IAppsFlyerValidateAndLog
    {
        public enum Mode
        {
            /// <summary>Không làm gì. Dùng khi Purchase Connector đã tự log revenue.</summary>
            Disabled = 0,

            /// <summary>
            /// Gửi qua validateAndSendInAppPurchase: AppsFlyer verify receipt với store rồi mới
            /// ghi nhận af_purchase. Tự lùi về <see cref="LogEventOnly"/> khi không dựng được
            /// receipt (editor, store chưa init, receipt rỗng).
            /// </summary>
            ValidateAndLog = 1,

            /// <summary>
            /// Bắn thẳng af_purchase, không validate receipt. Không chống được gian lận,
            /// chỉ dùng khi ValidateAndLog không chạy.
            /// </summary>
            LogEventOnly = 2,
        }

        [Tooltip("Mặc định Disabled. Chỉ bật khi đã xác nhận Purchase Connector không log revenue.")]
        [SerializeField] Mode _mode = Mode.Disabled;

        [SerializeField] bool _verboseLog;

        void OnEnable()
        {
            if (_mode == Mode.Disabled) { return; }

            InAppPurchaseHelper.onPayoutSuccess += OnPayoutSuccess;
        }

        void OnDisable()
        {
            InAppPurchaseHelper.onPayoutSuccess -= OnPayoutSuccess;
        }

        void OnPayoutSuccess(PurchaseResultArgs args)
        {
            IAPProductData productData = InAppPurchaseHelper.GetProductData(args.productID);
            Product product = InAppPurchaseHelper.Instance.GetProduct(args.productID);

            if (!TryGetRevenue(product, productData, out double revenue, out string currency))
            {
                Debug.LogError($"[AppsFlyerIAP] Bỏ qua '{args.productID}': không lấy được giá từ store và defaultPrice = 0. Đặt defaultPrice trong ProductData để có đường lùi.");
                return;
            }

            if (_mode == Mode.ValidateAndLog
                && TrySendValidated(args.productID, product, productData, revenue, currency))
            {
                return;
            }

            SendEvent(args.productID, revenue, currency);
        }

        /// <summary>
        /// Giá thật user trả lấy từ store; chỉ lùi về defaultPrice/USD khi store chưa trả về
        /// metadata. Trả false khi cả hai đều không có giá — thà không bắn còn hơn bắn revenue 0.
        /// </summary>
        static bool TryGetRevenue(Product product, IAPProductData productData, out double revenue, out string currency)
        {
            if (product != null)
            {
                revenue = (double)product.metadata.localizedPrice;
                currency = product.metadata.isoCurrencyCode;
                if (revenue > 0d && !string.IsNullOrEmpty(currency)) { return true; }
            }

            revenue = productData.defaultPrice;
            currency = "USD";
            return revenue > 0d;
        }

        bool TrySendValidated(string productId, Product product, IAPProductData productData, double revenue, string currency)
        {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            if (product == null || !product.hasReceipt)
            {
                Debug.LogWarning($"[AppsFlyerIAP] '{productId}' không có receipt, lùi về bắn event không validate.");
                return false;
            }

            Dictionary<string, string> additionalDetails = BuildParams(productId, revenue, currency);
            bool isSubscription = productData.productType == ProductType.Subscription;

#if UNITY_IOS
            if (string.IsNullOrEmpty(product.transactionID))
            {
                Debug.LogWarning($"[AppsFlyerIAP] '{productId}' thiếu transactionID, lùi về bắn event không validate.");
                return false;
            }

            var iosDetails = AFSDKPurchaseDetailsIOS.Init(
                productId,
                product.transactionID,
                isSubscription ? AFSDKPurchaseType.Subscription : AFSDKPurchaseType.OneTimePurchase);
            AppsFlyer.validateAndSendInAppPurchase(iosDetails, additionalDetails, this);
#else
            if (!TryGetGooglePurchaseToken(product.receipt, out string purchaseToken))
            {
                Debug.LogWarning($"[AppsFlyerIAP] '{productId}' không đọc được purchaseToken, lùi về bắn event không validate.");
                return false;
            }

            var androidDetails = new AFPurchaseDetailsAndroid(
                isSubscription ? AFPurchaseType.Subscription : AFPurchaseType.OneTimePurchase,
                purchaseToken,
                productId);
            AppsFlyer.validateAndSendInAppPurchase(androidDetails, additionalDetails, this);
#endif

            if (_verboseLog) { Debug.Log($"[AppsFlyerIAP] validateAndSendInAppPurchase {productId} {revenue} {currency}"); }
            return true;
#else
            return false;
#endif
        }

        void SendEvent(string productId, double revenue, string currency)
        {
            if (!AppsFlyerWrapper.Initialized)
            {
                Debug.LogError($"[AppsFlyerIAP] AppsFlyer chưa init xong, af_purchase của '{productId}' bị bỏ.");
                return;
            }

            AppsFlyerWrapper.LogEvent(AFInAppEvents.PURCHASE, BuildParams(productId, revenue, currency));
            if (_verboseLog) { Debug.Log($"[AppsFlyerIAP] af_purchase {productId} {revenue} {currency}"); }
        }

        /// <summary>
        /// InvariantCulture là bắt buộc: locale châu Âu format 1.99 thành "1,99", AppsFlyer
        /// parse sai và revenue lệch hàng trăm lần.
        /// </summary>
        static Dictionary<string, string> BuildParams(string productId, double revenue, string currency)
        {
            return new Dictionary<string, string>
            {
                { AFInAppEvents.REVENUE, revenue.ToString(CultureInfo.InvariantCulture) },
                { AFInAppEvents.CURRENCY, currency },
                { AFInAppEvents.CONTENT_ID, productId },
                { AFInAppEvents.QUANTITY, "1" },
            };
        }

        #region Google receipt
        [Serializable]
        class UnityReceipt
        {
            public string Payload;
        }

        [Serializable]
        class GooglePayload
        {
            public string json;
        }

        [Serializable]
        class GooglePurchase
        {
            public string purchaseToken;
        }

        /// <summary>
        /// Receipt của Unity IAP là JSON lồng: wrapper -> Payload -> json -> purchaseToken.
        /// </summary>
        static bool TryGetGooglePurchaseToken(string receipt, out string purchaseToken)
        {
            purchaseToken = null;

            UnityReceipt unityReceipt = JsonUtility.FromJson<UnityReceipt>(receipt);
            if (unityReceipt == null || string.IsNullOrEmpty(unityReceipt.Payload)) { return false; }

            GooglePayload payload = JsonUtility.FromJson<GooglePayload>(unityReceipt.Payload);
            if (payload == null || string.IsNullOrEmpty(payload.json)) { return false; }

            GooglePurchase purchase = JsonUtility.FromJson<GooglePurchase>(payload.json);
            if (purchase == null) { return false; }

            purchaseToken = purchase.purchaseToken;
            return !string.IsNullOrEmpty(purchaseToken);
        }
        #endregion

        #region IAppsFlyerValidateAndLog
        public void onValidateAndLogComplete(string result)
        {
            Debug.Log($"[AppsFlyerIAP] validate complete: {result}");
        }

        /// <summary>
        /// Validate hỏng thì AppsFlyer không ghi nhận revenue. Cố tình không lùi về bắn event
        /// trần ở đây — làm vậy là vứt bỏ lớp chống gian lận vừa dựng lên.
        /// </summary>
        public void onValidateAndLogFailure(string error)
        {
            Debug.LogError($"[AppsFlyerIAP] validate failed: {error}");
        }
        #endregion
    }
}
