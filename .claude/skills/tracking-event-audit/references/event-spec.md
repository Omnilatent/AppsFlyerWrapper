# Event Spec — QC Test Cases (JaCat)

> Nguồn: **test case chính thức từ tester JaCat** (checklist Revenue → phần AppsFlyer/Tracking).
> Đây là các case QC dùng để nghiệm thu, nên audit đối chiếu trực tiếp với chúng. Vẫn có thể
> thiếu event ngoài Ads (Firebase `ad_impression`, DataBucket...) — bổ sung khi team cung cấp
> schema riêng. Với phạm vi Ads revenue + lifecycle, đây là spec đối chiếu.

## Quy ước cột

- **Event**: tên event kỳ vọng.
- **Khi bắn**: điều kiện phát.
- **Param bắt buộc**: key + giá trị/kiểu mong đợi.
- **Ghi chú**: chỗ dễ lệch, cần đối chiếu kỹ.

## TC-1 · Có đủ event `ad_show` và `in_background`

Yêu cầu **bắt buộc** với mọi game JaCat: app phải luôn gửi cả hai event `ad_show` và
`in_background`.

| Event | Khi bắn | Param bắt buộc | Ghi chú |
|---|---|---|---|
| `ad_show` | Mỗi lần show ads thành công | (xem TC-3) | Tên phải đúng `ad_show`. Code wrapper mặc định phát `show_ad` (đảo chữ) khi caller không truyền `eventName` — phải kiểm caller có truyền `eventName="ad_show"` không. |
| `in_background` | Khi app mất focus / vào nền | (không bắt buộc param) | Wrapper bắn qua `eventLogOnAppLoseFocus` (mặc định `"in_background"`) trong `OnApplicationFocus(false)`. Kiểm giá trị field này không bị đổi. |

## TC-2 · `ad_show` gửi đủ tương ứng TỪNG placement

`ad_show` phải bắn cho **mọi placement/format** đã tích hợp (Interstitial, Reward, Open, Native,
Banner, ...). Thiếu một nhánh format = thiếu event cho placement đó.

**Quan trọng — verdict này nằm ở HOST, không ở wrapper.** `TrackRevenueAdmob/MAX` do code game /
AdsManager gọi từ callback Ad-Paid của từng format. Wrapper chỉ chứng minh *đường ống tồn tại*;
"đủ mọi placement" phải audit ở project game (chỗ đăng ký callback Ad-Paid từng format). Xem
"Ranh giới wrapper vs host" trong SKILL.md.

## TC-3 · `ad_show` đầy đủ param

Bắt buộc có các param:

| Param (spec) | Giá trị mong đợi | Đối chiếu trong code |
|---|---|---|
| **Revenue** | Số dương (QC test = 0, QC thật > 0) | Wrapper gắn key `revenue` (hằng `REVENUE_PARAM_NAME`), **chỉ khi** `logAdRevenueAsEvent == true`. Giá trị là chuỗi thập phân `0.0000000` (`RevenueToString`), Admob đã chia 1e6. **Tester ghi "số nguyên dương" nhưng code phát số thập phân nhỏ** — cần làm rõ ý tester (số nguyên hay chỉ cần > 0). |
| **Currency** | Thường USD | Wrapper gắn key `currency` (hằng `AFInAppEvents.CURRENCY`) = `currencyCode` caller truyền, **chỉ khi** `logAdRevenueAsEvent`. Lưu ý: API `AppsFlyer.logAdRevenue` bên trong lại hardcode `"USD"` bất kể `currencyCode`. |
| **Placement** | JaCat đặt param tên **`ad_unit_id`**, giá trị đại diện vị trí ads: `"101"`, `"301"`, ... | **Không phải AdMob unit id thật.** Giá trị vị trí này do caller truyền qua `additionalData` — wrapper không tự set. Phải đối chiếu ở host xem `additionalData["ad_unit_id"]` có mang "101"/"301" không, đừng nhầm với unit id AdMob. |
| **Content Type** | `"MAX"` hoặc `"AdMob"` | Wrapper gắn key `content_type` (hằng `AFInAppEvents.CONTENT_TYPE`) = `"admob_revenue"`/`"max_revenue"` — **khác literal tester mong đợi**. TikTok tracker (`af_ad_revenue2`) lại dùng `monetization_network` = `googleadmob`/`applovinmax`. Đây là điểm ⚠️ gần như chắc chắn lệch. |

## TC-4 · Check value Revenue của `ad_show`

- Bản **QC test**: `value = 0`.
- Bản **QC thật**: `value > 0`. Nếu `revenue` không trả về hoặc = 0 ở bản thật → **bug, báo dev**.

Audit tĩnh chỉ chốt được: param `revenue` *có được gắn* và ở bản debug logic cho ra 0. Phần
`value > 0` thật chỉ verify được sau khi ads live + app lên store → thuộc mục "Cần xác nhận
hậu-store" của báo cáo.

## Khoảng trống đã biết (cần spec team lấp)

- 4 TC trên chỉ phủ `ad_show` + `in_background` + bộ param revenue. Nếu team dùng thêm event
  Firebase (`ad_impression`, `paid_ad_*`...) hay DataBucket, cần bổ sung schema riêng.
- Ý nghĩa chính xác "Revenue là số nguyên dương" (TC-3) — số nguyên thật hay chỉ cần dương? Code
  phát thập phân.
- Literal `content_type` team thực sự chấp nhận: `"MAX"`/`"AdMob"` hay `"max_revenue"`/`"admob_revenue"`?
- Danh sách placement_id hợp lệ ("101","301",...) và map format ↔ placement.
