# AppsFlyerWrapper

# Dependencies
- [appsflyer-unity-plugin 6.10.x+](https://github.com/AppsFlyerSDK/appsflyer-unity-plugin).
- [appsflyer-unity-adrevenue-generic-connector 6.9.42+](https://github.com/AppsFlyerSDK/appsflyer-unity-adrevenue-generic-connector)
- [appsflyer-unity-purchase-connector 1.0.x+](https://github.com/AppsFlyerSDK/appsflyer-unity-purchase-connector).
- Firebase Cloud Messaging.
- Unity's Mobile Notification.

# Kết hợp với các thư viện khác
Để thu thập thông tin revenue từ Admob/MAX:

Trong sự kiện Ad Paid nhận được từ SDK quảng cáo (đối với thư viện AdsManager thì sự kiện được bắn trong class HandleAdmobMessage hoặc HandleMAXMessage), gọi hàm TrackRevenueAdmob() / TrackRevenueMAX() và truyền vào param tương thích.
- Hàm TrackRevenueAdmob() sẽ convert giá trị gốc của Admob về giá trị đô la tương ứng nên bạn chỉ cần truyền thẳng giá trị gốc vào, không cần chia cho 1 triệu.

# Doanh thu IAP

Mặc định doanh thu IAP do **AppsFlyer Purchase Connector** tự log (bật trong `AppsFlyerWrapper.ConfigurePurchaseConnector`). Không cần code gì thêm từ phía game.

Nếu xác nhận được connector im lặng — phải bằng log device, không phải suy luận từ code — thì copy `Templates~/AppsFlyerIapRevenueBridge.cs` vào project, gắn `AppsFlyerIapRevenueBridge` lên GameObject sống xuyên scene (đặt luôn cạnh `AppsFlyer Wrapper` là gọn nhất) và chọn `Mode`:

| Mode | Khi nào dùng |
|---|---|
| `Disabled` (mặc định) | Purchase Connector đang chạy đúng |
| `ValidateAndLog` | Connector im lặng. AppsFlyer verify receipt với store rồi mới ghi `af_purchase` |
| `LogEventOnly` | `ValidateAndLog` không chạy được. Bắn `af_purchase` trần, không chống gian lận |

**Không bật bridge cùng lúc với Purchase Connector đang hoạt động** — doanh thu sẽ bị đếm hai lần. Đây là bước chuyển dứt điểm, không phải chạy song song.

Bridge móc vào `InAppPurchaseHelper.onPayoutSuccess` nên cần `Omnilatent.InAppPurchaseHelper` 2.5.4+.

## Trước khi kết luận connector hỏng

Kiểm tra `setIsSandbox` trước đã. `AppsFlyerWrapper` đang truyền `Debug.isDebugBuild`, mà build TestFlight / Google Play internal test là release build → cờ này `false` → AppsFlyer validate receipt sandbox với server production → **toàn bộ event bị drop**. Triệu chứng giống hệt "connector không log revenue".

# Known Issues
AppsFlyer và Firebase đã từng gây ra vấn đề làm chặn Coroutine trong scene đầu tiên. Nếu như game gặp vấn đề với Coroutine trong scene đầu và làm game bị soft lock, hãy đổi hết Coroutine sang C# Task hoặc UniTask trong scene đầu tiên.
