<#
.SYNOPSIS
  Gom bằng chứng TĨNH cho tracking-event-audit: liệt kê nơi phát analytics event, các literal
  tên event, độ phủ theo spec (event + param), và red flag (ID ads test / ad unit rỗng).
  Chỉ ĐỌC, không sửa gì. In ngắn gọn (file:line + đếm), không dump cả file.

.PARAMETER ProjectRoot
  Thư mục gốc project Unity. Mặc định "." .

.PARAMETER Events
  Danh sách tên event cần đối chiếu độ phủ (theo spec). Vd: ad_show,show_ad,in_background

.PARAMETER Params
  Danh sách tên param cần đối chiếu độ phủ. Vd: revenue,currency,placement,ad_unit_id,content_type

.PARAMETER ScanRoot
  Thư mục con để quét (mặc định "Assets"). Không quét Library/Temp/obj/... .

.EXAMPLE
  pwsh -File scan-tracking.ps1 -ProjectRoot . -Events ad_show,show_ad,in_background -Params revenue,currency,placement,ad_unit_id,content_type
#>
param(
    [string]$ProjectRoot = ".",
    [string[]]$Events = @("ad_show", "show_ad", "in_background"),
    [string[]]$Params = @("revenue", "currency", "placement", "ad_unit_id", "content_type", "af_content_type"),
    [string]$ScanRoot = "Assets"
)

$ErrorActionPreference = "Stop"
$root = Join-Path (Resolve-Path $ProjectRoot) $ScanRoot
if (-not (Test-Path $root)) { Write-Error "Không thấy thư mục quét: $root"; exit 1 }

# Bỏ qua cache/generated (do-not-do.md)
$excludeDirs = @("\Library\", "\Temp\", "\obj\", "\.vs\", "\Logs\", "\UserSettings\", "\Bee\")
$csFiles = Get-ChildItem -Path $root -Recurse -File -Filter *.cs |
    Where-Object { $p = $_.FullName; -not ($excludeDirs | Where-Object { $p -like "*$_*" }) }

function Search-Pattern {
    param([string]$Pattern, [switch]$Regex)
    $hits = @()
    foreach ($f in $csFiles) {
        $n = 0
        foreach ($line in [System.IO.File]::ReadLines($f.FullName)) {
            $n++
            $match = if ($Regex) { $line -match $Pattern } else { $line.Contains($Pattern) }
            if ($match) {
                $rel = $f.FullName.Substring((Resolve-Path $ProjectRoot).Path.Length).TrimStart('\', '/')
                $hits += [pscustomobject]@{ File = $rel; Line = $n; Text = $line.Trim() }
            }
        }
    }
    $hits
}

function Show-Hits {
    param($Hits, [int]$Max = 12)
    if (-not $Hits -or $Hits.Count -eq 0) { Write-Output "    (không tìm thấy)"; return }
    $Hits | Select-Object -First $Max | ForEach-Object {
        $t = if ($_.Text.Length -gt 110) { $_.Text.Substring(0, 109) + "…" } else { $_.Text }
        Write-Output ("    {0}:{1}  {2}" -f $_.File, $_.Line, $t)
    }
    if ($Hits.Count -gt $Max) { Write-Output ("    … +{0} dòng nữa" -f ($Hits.Count - $Max)) }
}

Write-Output "===== TRACKING EVENT SCAN ====="
Write-Output ("Quét: {0}  ·  {1} file .cs" -f $root, $csFiles.Count)

# --- A. Emit sites: nơi thực sự phát event ra SDK ---
Write-Output "`n--- A. Emit sites (nơi phát event) ---"
$emitRegex = 'sendEvent\(|logAdRevenue\(|\.LogEvent\(|TrackRevenue(Admob|MAX)\(|AppsFlyer\.sendEvent'
$emit = Search-Pattern -Pattern $emitRegex -Regex
Show-Hits $emit 30

# --- B. Event-name literals (default eventName, chuỗi gán) ---
Write-Output "`n--- B. Event-name literals (eventName / default) ---"
$nameRegex = 'eventName\s*=|"(show_ad|ad_show)"|eventLogOnAppLoseFocus'
Show-Hits (Search-Pattern -Pattern $nameRegex -Regex) 20

# --- C. Spec coverage: từng event ---
Write-Output "`n--- C. Độ phủ EVENT theo spec ---"
foreach ($e in $Events) {
    $h = @(Search-Pattern -Pattern ('"{0}"' -f $e))
    Write-Output ("  [event] {0}  →  {1} hit" -f $e, $h.Count)
    Show-Hits $h 6
}

# --- D. Spec coverage: từng param ---
# Param có thể được truyền dưới 3 dạng: literal "currency", hằng SDK AFInAppEvents.CURRENCY,
# hoặc hằng nội bộ (REVENUE_PARAM_NAME). Codebase này gắn param CHỦ YẾU qua hằng, nên quét literal
# đơn thuần sẽ báo thiếu giả. Vì vậy mỗi param quét cả literal + token hằng UPPER tương ứng.
Write-Output "`n--- D. Độ phủ PARAM theo spec (literal + hằng) ---"
foreach ($p in $Params) {
    $upper = $p.ToUpper()                      # currency -> CURRENCY ; content_type -> CONTENT_TYPE
    # OR: "param" | .CURRENCY | .CONTENT_TYPE | REVENUE_PARAM_NAME-style hằng chứa token
    $pat = '"{0}"|\.{1}\b|\b\w*{1}\w*\b\s*,' -f [regex]::Escape($p), $upper
    $h = @(Search-Pattern -Pattern $pat -Regex)
    Write-Output ("  [param] {0}  →  {1} hit (literal+hằng)" -f $p, $h.Count)
    Show-Hits $h 6
}

# --- D2. Giá trị literal quan trọng (bắt content_type / network dù KEY là hằng) ---
Write-Output "`n--- D2. Giá trị literal (content_type / monetization network) ---"
$valueTokens = @("admob_revenue", "max_revenue", "googleadmob", "applovinmax", '"AdMob"', '"MAX"', "af_ad_revenue")
foreach ($v in $valueTokens) {
    $h = @(Search-Pattern -Pattern $v)
    Write-Output ("  [value] {0}  →  {1} hit" -f $v, $h.Count)
    Show-Hits $h 4
}

# --- E. Red flags ---
Write-Output "`n--- E. Red flags ---"
Write-Output "  Google TEST ad unit id (3940256099942544):"
Show-Hits (Search-Pattern -Pattern "3940256099942544") 20
Write-Output "  Ad unit / key có thể rỗng-placeholder:"
Show-Hits (Search-Pattern -Pattern 'adUnitId\s*=\s*""|placement\s*=\s*""|devKey\s*=\s*"";?\s*$|"YOUR_|"xxxx' -Regex) 20

Write-Output "`n===== HẾT SCAN ====="
Write-Output "Bước tiếp: đọc code tại các emit site (mục A/B) để hiểu ngữ nghĩa — tên event có thể bị"
Write-Output "map/đổi, param có thể mang giá trị ở key khác. Rồi xuất báo cáo theo khung trong SKILL.md."
