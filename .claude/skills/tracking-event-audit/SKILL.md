---
name: tracking-event-audit
description: >
  Audit TĨNH (đọc code, không cần device/không cần lên store) các event tracking analytics
  AppsFlyer của app so với SPEC test case của QC/team, rồi bung ra bảng đối chiếu Pass/Lệch/Thiếu
  kèm file:line. Dùng khi QC/dev cần kiểm "app có bắn đủ event đúng tên + đủ param + đúng giá trị
  mong đợi không" TRƯỚC KHI phát hành, đặc biệt các case trong checklist Revenue mà bình thường
  "phải lên store mới check được" (vd event ad_show/show_ad, in_background, param
  revenue/currency/placement/ad_unit_id/content_type, revenue=0 ở bản test). Kích hoạt khi người
  dùng nói tới: kiểm event AppsFlyer, audit tracking, check event ad_show / in_background, đối
  chiếu event với tài liệu/spec, "ID/param tracking đúng chưa", pre-test tracking, hoặc soi
  placement_id/ad_unit_id trong event revenue. Event Firebase (`ad_impression`...) / DataBucket
  CHƯA có spec ở đây — chỉ audit khi team cấp schema riêng. Đây là audit code, KHÔNG chạy app và
  KHÔNG thay việc verify revenue thật trên dashboard sau khi lên store.
---

# Tracking Event Audit

Kiểm tra **bằng cách đọc code** xem app có bắn đúng các analytics event theo **spec của team** hay
không. Mục tiêu: gánh bớt phần lặp cho QC — những gì suy ra được từ source (tên event, đủ param,
giá trị literal như currency="USD", content_type, revenue=0 ở bản debug) thì máy chốt trước, để QC
chỉ tập trung vào phần bắt buộc phải chạy thật.

## Ranh giới — nói thẳng để không hiểu lầm

Audit này **KHÔNG** thay được:

- **Revenue thật > 0**: chỉ có khi ads live + app đã lên store. Code chỉ cho biết param `revenue`
  *có được gắn* và *ở bản test bằng 0* hay không.
- **Event thực sự tới AppsFlyer/Firebase dashboard**: cần project trên store. Muốn xác nhận event
  *bắn ra runtime* trước store thì phải bắt log tại chỗ (xem [references/runtime-logcat.md](references/runtime-logcat.md)) — đó là bước bán tự động, ngoài phạm vi audit tĩnh này.

Cái audit này chốt được: **tên event đúng spec, đủ param bắt buộc, giá trị literal đúng, param không
rỗng, không lọt ID test.** Riêng "có wire cho mọi format/placement" (TC-2) phần lớn nằm ở HOST, không
ở wrapper — xem "Ranh giới wrapper vs host". Với mỗi kết luận luôn kèm file:line để QC/dev soi lại.

## Ranh giới wrapper vs host — đọc trước khi phán TC-2

`AppsFlyerWrapper` chỉ là **đường ống**. `TrackRevenueAdmob/MAX` (nơi bắn `ad_show`) do **code game /
AdsManager** gọi từ callback Ad-Paid của từng format. Nên chia verdict làm 2 loại:

- **Thuộc wrapper** (audit ngay trong submodule này): tên event mặc định, key/giá trị param wrapper
  tự gắn (`revenue`, `currency`, `content_type`), điều kiện `logAdRevenueAsEvent`, event
  `in_background`.
- **Thuộc host** (phải audit ở project game, wrapper KHÔNG phán được): `ad_show` có được gọi cho
  **mọi** placement/format không (TC-2), giá trị `additionalData["ad_unit_id"]` = "101"/"301" caller
  truyền vào có đúng không (TC-3 phần Placement), caller có truyền `eventName="ad_show"` để đè default
  `show_ad` không.

Khi audit chỉ trong submodule wrapper, đánh dấu các verdict host là **❓ Chưa xác định (thuộc host)**
thay vì phán Pass/Thiếu. Muốn phán được thì mở rộng scan sang code game.

## Nguồn chân lý: SPEC event

Audit là **so app với spec**, nên không có spec thì không phán được Pass/Fail — chỉ liệt kê được
"app đang bắn gì".

- Spec sống ở [references/event-spec.md](references/event-spec.md) — hiện là **test case QC của
  JaCat** (TC-1…TC-4: `ad_show` + `in_background` + bộ param revenue).
- Nếu team đưa spec đầy đủ hơn (thêm event Firebase, DataBucket, schema chi tiết), cập nhật
  `event-spec.md` rồi mới chạy. Ghi rõ trong báo cáo đang đối chiếu theo spec nào.

## Cách gọi (skill sống trong submodule wrapper)

Skill này **đi theo submodule `AppsFlyerWrapper`**, không nằm ở `.claude/skills` gốc game → Claude
**không auto-discover** khi mở game project. Gọi thủ công: trỏ thẳng tới path này, hoặc chạy scanner
trực tiếp (Bước 2). Luôn audit **từ gốc game project** để phủ code host, không chỉ submodule.

## Quy trình

### Bước 1 — Chốt spec + inventory placement của game
1. Đọc `references/event-spec.md` (TC-1…TC-4). Nếu team có spec đầy đủ hơn, merge vào rồi tiếp.
2. **Lấy danh sách placement hợp lệ + map format của GAME ĐANG AUDIT** — cần cho TC-2 và phần
   Placement của TC-3. Nguồn: config placement của game (vd JacatAds `NetworkSetting.AdsFormatIDs` /
   `PlacementIDs.cs`, hoặc nơi game định nghĩa "101"/"301"). Không có inventory này thì TC-2 chỉ ra
   được ❓, không chốt "đủ mọi placement" được.
3. Xác định phạm vi: toàn bộ event hay chỉ nhóm nào (vd chỉ Ads revenue, chỉ lifecycle).

### Bước 2 — Gom bằng chứng bằng scanner
Chạy scanner để lấy nhanh mọi điểm phát event + literal + red flag, tránh grep tay nhiều lần:

```powershell
pwsh -File Assets/Omnilatent/AppsFlyerWrapper/.claude/skills/tracking-event-audit/scripts/scan-tracking.ps1 `
  -ProjectRoot . `
  -Events ad_show,show_ad,in_background `
  -Params revenue,currency,placement,ad_unit_id,content_type,af_content_type
```

(Chạy từ gốc project Unity. Nếu audit từ trong submodule wrapper thì `-ProjectRoot` trỏ về gốc để
scan cả code host — nếu không, TC-2 và phần Placement sẽ ra 0 hit vì chúng nằm ở host.)

Script in ra (ngắn gọn, không dump cả cây):
- **A. Emit sites**: nơi gọi API phát event (`sendEvent`, `logAdRevenue`, `*.LogEvent(`, `TrackRevenueAdmob/MAX`) + file:line.
- **B. Event-name literals**: chuỗi tên event tìm thấy quanh các emit site.
- **C/D. Spec coverage**: mỗi event / param → số hit + vài file:line. Param quét **cả literal lẫn
  hằng SDK** (`currency` khớp cả `AFInAppEvents.CURRENCY`) — codebase gắn param chủ yếu qua hằng, quét
  literal đơn thuần sẽ báo thiếu giả.
- **D2. Giá trị literal**: `admob_revenue`/`max_revenue`/`googleadmob`/`applovinmax`/`"MAX"`/`"AdMob"` —
  bắt `content_type`/network dù KEY là hằng, để so trực tiếp với literal spec mong đợi.
- **E. Red flags**: ID ads test của Google (`3940256099942544`), ad unit rỗng/placeholder.

Nếu không có `pwsh`/`powershell`, tự grep tay theo đúng các pattern trên (đừng đụng `Library/`,
`Temp/`, `obj/` — theo `do-not-do.md`).

### Bước 3 — Đọc code tại các emit site
Scanner chỉ ra chỗ; đọc để hiểu **ngữ nghĩa**, vì tên event có thể bị map/đổi. Các bẫy hay gặp
(đã gặp thật trong dự án này — dùng làm ví dụ, không phải danh sách đóng):

- **Tên event bị đổi/đảo chữ**: spec ghi `ad_show` nhưng code phát `show_ad` (default eventName
  trong `TrackRevenueAdmob/MAX`). Đảo thứ tự chữ = vẫn là lệch, phải bắt.
- **Param mang giá trị ở KEY khác**: spec kỳ vọng `ad_unit_id` chứa "101"/"301", nhưng code để
  giá trị đó ở param `placement`, còn `ad_unit_id` lại là AdMob unit id thật. Phải đối chiếu
  *giá trị*, không chỉ tên key.
- **Giá trị literal khác mô tả**: spec ghi content type "MAX"/"AdMob", code gắn
  `af_content_type = "max_revenue"/"admob_revenue"`.
- **Param chỉ gắn có điều kiện**: vd `revenue`/`currency` chỉ thêm khi một cờ như
  `logAdRevenueAsEvent == true`. Ghi rõ điều kiện.
- **Event có gọi cho MỌI format/placement không (TC-2, ở HOST)**: tìm mọi call-site
  `TrackRevenueAdmob(`/`TrackRevenueMAX(` trong code game (thường ở AdsManager
  `HandleAdmobMessage`/`HandleMAXMessage` + callback Ad-Paid từng format). Đối chiếu với inventory
  placement lấy ở Bước 1 — thiếu format nào = thiếu `ad_show` cho placement đó. Tại mỗi call-site
  còn kiểm: caller có truyền `eventName="ad_show"` (đè default `show_ad`) không, và
  `additionalData["ad_unit_id"]` có mang giá trị vị trí "101"/"301" không.

### Bước 4 — Xuất báo cáo
Dùng đúng khung ở mục dưới. Mỗi dòng là một yêu cầu trong spec, kèm verdict + bằng chứng.

## Khung báo cáo (bắt buộc theo mẫu này)

```markdown
# Tracking Event Audit — <AppName>

Spec đối chiếu: <event-spec.md — QC test case TC-1…TC-4 | spec team nếu có>
Phạm vi scan: <chỉ submodule wrapper | cả code host (gốc project)>

## Tổng quan
- Pass: N  ·  Lệch (⚠️): N  ·  Thiếu (❌): N  ·  Chưa xác định (❓): N

## Chi tiết
| TC | Spec yêu cầu | Trạng thái | Thực tế trong code | Bằng chứng |
|---|---|---|---|---|
| TC-1 | Event `ad_show` được bắn mỗi impression | ⚠️ Lệch | default eventName là `show_ad` (đảo chữ); phải xem caller có đè `eventName="ad_show"` | AppsFlyerWrapper.cs:168, :205 |
| TC-1 | Event `in_background` khi mất focus | ✅ | `OnApplicationFocus(false)` bắn `eventLogOnAppLoseFocus` | AppsFlyerWrapper.cs:300 |
| TC-2 | `ad_show` đủ mọi placement | ❓ thuộc host | wrapper chỉ là ống; caller từng format quyết định | (audit ở project game) |
| TC-3 | Param `revenue` (0 ở test) | ✅ | gắn `revenue`, chỉ khi `logAdRevenueAsEvent` | AppsFlyerWrapper.cs:22, :177 |
| TC-3 | Param `content_type` = "MAX"/"AdMob" | ⚠️ Lệch | gắn `admob_revenue`/`max_revenue` | AppsFlyerWrapper.cs:161, :198 |
| ... | | | | |

## Cần xác nhận runtime / hậu-store (audit tĩnh không phán được)
- <vd: revenue>0 thật, event tới dashboard — QC làm sau khi lên store>

## Verdict thuộc host (cần scan code game để chốt)
- <TC-2, giá trị ad_unit_id="101"/"301", caller có đè eventName không>

## Điểm chưa chắc (declare-uncertainty)
- <chỗ nào đọc code chưa đủ để kết luận, cần spec hoặc thông tin thêm>
```

Ký hiệu: **✅ Pass** = khớp spec; **⚠️ Lệch** = có bắn nhưng tên/param/giá trị khác spec;
**❌ Thiếu** = spec yêu cầu nhưng không tìm thấy; **❓ Chưa xác định** = bằng chứng chưa đủ *hoặc
verdict thuộc host chưa scan tới*, nói thẳng thay vì đoán (theo `declare-uncertainty.md`).

## Nguyên tắc

- **Không tự quyết đúng/sai khi spec chưa rõ.** Lệch tên có thể do app khác truyền `eventName`
  đè, do checklist viết lỏng, hoặc bug thật — nêu cả khả năng, đừng chốt liều.
- **Luôn kèm file:line.** Báo cáo không có bằng chứng = vô dụng cho QC.
- **Chỉ đọc, không sửa code** (theo `check-only.md`) trừ khi người dùng yêu cầu fix.
- **Token economy**: scanner in số liệu + vài file:line, không dump cả file/hierarchy.
