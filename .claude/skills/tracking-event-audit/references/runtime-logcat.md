# Runtime capture — bắt event TRƯỚC store (tầng 2, bán tự động)

Audit tĩnh (SKILL.md) chỉ đọc code. Muốn xác nhận event **thực sự bắn ra runtime** mà **chưa lên
store**, bắt log tại chỗ trên bản debug thay vì chờ dashboard AppsFlyer. Đây là bước bán tự động:
người test phải thao tác show ads, skill/parser lo phần đối chiếu.

## Vì sao làm được trước store

- Project AppsFlyer chỉ sinh sau khi app lên store → **dashboard** không dùng được trước đó.
- Nhưng SDK vẫn **gửi event** ngay từ bản debug; nó in ra console/logcat khi `setIsDebug(true)`.
  Trong repo này `AppsFlyerWrapper.Initialize()` gọi `AppsFlyer.setIsDebug(Debug.isDebugBuild)`.
- Với ads test → `revenue = 0` (đúng như checklist "QC test value = 0"), nên vẫn kiểm được
  đủ-param + đúng-tên, chỉ không kiểm được số tiền thật.

## Cách bắt (Android)

```bash
# Lọc log AppsFlyer khi bấm show từng loại ads trên bản debug
adb logcat -c
adb logcat | Select-String -Pattern "AppsFlyer|show_ad|in_background|logAdRevenue"
```

- iOS: xem Xcode console / Console.app, lọc "AppsFlyer".
- Editor Play mode: bật `debugLogEvent` trên component `AppsFlyerWrapper` → log ra Unity Console
  dạng `AppsFlyer log: <name>, <param>, <value>` (chỉ literal, không đi mạng).

## Quy trình kiểm

1. Build debug, cài lên máy/emulator.
2. `adb logcat -c` rồi bắt đầu bắt log.
3. Show **lần lượt từng placement** (Inter, Reward, Open, Native, Banner...).
4. Dán đoạn log bắt được cho skill → skill parse ra: mỗi lần show có event tên gì, param nào,
   `revenue` có = 0 không, `placement`/`ad_unit_id`/`content_type` có mặt không.
5. Đối chiếu với spec → cùng khung báo cáo Pass/⚠️/❌ như audit tĩnh.

## Vẫn không kiểm được ở đây

- `revenue > 0` thật (cần ads live + store).
- Event có tới đúng dashboard/attribution không (cần project store).

Hai cái này để lại cho QC sau khi phát hành — ghi rõ trong báo cáo mục "Cần xác nhận hậu-store".
