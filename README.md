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

# Known Issues
AppsFlyer và Firebase đã từng gây ra vấn đề làm chặn Coroutine trong scene đầu tiên. Nếu như game gặp vấn đề với Coroutine trong scene đầu và làm game bị soft lock, hãy đổi hết Coroutine sang C# Task hoặc UniTask trong scene đầu tiên.
