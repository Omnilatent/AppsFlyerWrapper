# 1.2.0
Thêm `Templates~/AppsFlyerIapRevenueBridge.cs`: component tùy chọn để bắn doanh thu IAP lên AppsFlyer từ phía game, dùng cho project mà Purchase Connector không tự log revenue.
- Mặc định `Disabled`. Bật khi Purchase Connector vẫn chạy đúng sẽ làm doanh thu bị đếm hai lần.
- `ValidateAndLog` (khuyến nghị): gửi qua `validateAndSendInAppPurchase`, AppsFlyer verify receipt với store trước khi ghi nhận `af_purchase`.
- `LogEventOnly`: bắn thẳng `af_purchase`, không validate.
- Yêu cầu `Omnilatent.InAppPurchaseHelper` 2.5.4+ (bản raise `onPayoutSuccess`).

# 1.1.0
Sửa tương thích với Appsflyer 6.15.0: Appsflyer Ad Revenue connector được tích hợp vào Appsflyer SDK. 

# 1.0.0
Bản đầu tiên