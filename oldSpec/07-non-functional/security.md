# 非功能需求：安全性

## 1. HTTP 回應標頭（首頁）[觀察]

| 標頭 | 值 | 評估 |
|------|-----|------|
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | ✅ |
| `X-Content-Type-Options` | `nosniff` | ✅ |
| `X-Frame-Options` | `SAMEORIGIN` | ✅ |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | ✅ |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=()` | ✅ |
| `Content-Security-Policy` | **未設定** | ⚠️ 應補上 |
| `Server` | `Apache/2` | ⚠️ 透露伺服器 |
| `X-Powered-By` | `PHP/8.2.13` | ⚠️ 透露精確版本，應移除 |

## 2. Session 與 Cookie

- 所有訪客（含未登入的公開頁）都會收到 Cookie `SYTTCM_ADMIN`（`Secure; HttpOnly; SameSite=Lax`）。
- Cookie 名稱含「ADMIN」，推測前後台共用同一個 session 名稱 [推論]。建議新版分開前台與後台的 session。
- 頁面設定 `Cache-Control: no-store, no-cache, must-revalidate`、`Expires: 1981`（PHP session 預設），因為每頁都含 CSRF token。

## 3. 表單防護

| 防護 | 現況 |
|------|------|
| CSRF | 隱藏欄位 `_token`（64 hex），與 session 綁定 [推論] |
| 機器人 | 蜜罐欄位 `website` |
| 速率限制 | 未知 |
| CAPTCHA | 無 |
| 輸入限制 | 前端 maxlength：name 80、phone 24、notes 2000 |

## 4. 後台

- `/admin`：301 轉址（推測導向登入頁），robots 禁止索引。
- `/preview/`：robots 禁止索引，推測為草稿預覽。

## 5. 個人資料

預約表單收集姓名、電話、預約與症狀描述（可能含健康資訊）。須符合香港《個人資料（私隱）條例》：

- 舊站只有一行同意文字「我同意診所使用以上資料處理及聯絡此預約。」，**沒有私隱政策頁**。
- 建議新版加入私隱政策（收集目的、保存期限、查閱及更正權利），並在表單同意文字中連結。

## 6. 外部連結

外部連結（WhatsApp、Google Maps）使用 `target="_blank" rel="noopener"`。
