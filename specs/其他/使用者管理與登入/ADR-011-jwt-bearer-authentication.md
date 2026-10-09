# ADR-011 登入改用 JWT Bearer（短效 access token＋可撤銷的 refresh token）

| 項目 | 內容 |
|------|------|
| 狀態 | Accepted |
| 日期 | 2026-10-08 |
| 決策者 | Wayne（維護者；選定 token 傳遞方式、效期續期方式，其餘細節授權 AI 決定） |
| 出處 | [SPEC-004](spec.md) Clarifications Session 2026-10-08（登入改 JWT）、變更（Delta）2026-10-08 |
| 取代 | [SPEC-004](spec.md) 設計決策 R1（Cookie 驗證）；FR-021 原「以 HttpOnly Cookie 承載登入工作階段」 |

> 📦 本 ADR 隨範本沿用自源專案（CHB），決策本身仍適用於範本。背景、影響與相關連結中提到的舊系統、分行、企業、開狀申請、系統參數等屬源專案業務，相關規格、資料表與 migration 範本已移除；以骨架程式碼與測試為準。

## 背景（Context）

SPEC-004（2026-08）以 ASP.NET Core Cookie 驗證承載登入狀態：HttpOnly、`SameSite=Lax`、8 小時滑動逾時，每個請求依帳號重新查詢角色與可見選單（設計決策 R8）。2026-10-08 使用者決定登入改用 JWT。API 由另一團隊的前端呼叫，改用 Bearer token 後，前端與其他呼叫端不再依賴瀏覽器 Cookie 帶身分。

要維持的既有性質：

- 身分憑證不可放在 JavaScript 讀得到的長期儲存（FR-021 防 XSS 的精神）。
- 持續操作就不被登出（8 小時滑動）。
- 登出、刪除帳號、移除角色、角色可見性變更要在下一個請求生效（R8）。

## 考慮過的選項

token 傳遞（使用者選定第 1 項）：

1. **Bearer 標頭＋前端存記憶體**；重新整理頁面時以 HttpOnly Cookie 中的 refresh token 換新 access token。
2. JWT 放 HttpOnly Cookie：前端幾乎不用改，但失去 Bearer 對非瀏覽器呼叫端的便利。
3. Bearer＋localStorage：XSS 可直接竊取 token，違反 FR-021。

效期與續期（使用者選定第 1 項）：

1. **access token 15 分鐘＋refresh token 8 小時滑動**，refresh token 存資料庫、每次使用即輪替、可撤銷。
2. 單一 8 小時 token、不續期：最簡單，但失去滑動逾時，登出無法在伺服器端生效。
3. 單一 token、每次回應換發：舊 token 在效期內仍有效、無法撤銷。

## 決策結果

**access token**

- JWT，HMAC-SHA256 簽章；效期 15 分鐘。
- claims 只放身分：`sub`（使用者對外識別碼）、`name`（帳號）、`sid`（session 對外識別碼）、`jti`、`iat`／`nbf`／`exp`，`iss`=`TcmClinic.Api`、`aud`=`TcmClinic.Web`。**角色與可見選單不放進 token**，每個請求照舊由資料庫查詢（R8），所以權限變更不必等 token 過期。
- 每個請求另檢查 `sid` 對應的 session 仍有效（未撤銷、未逾期）；登出或 session 被撤銷後，尚未過期的 access token 也立即失效。
- 簽章金鑰：設定 `Jwt:SigningKey`（Base64，至少 32 bytes）。非 Development 環境未設定即無法啟動；Development 未設定時於啟動產生隨機金鑰（重啟後舊 access token 失效，前端以 refresh token 換新即可），避免把金鑰寫進版控。

**refresh token**

- 32 bytes 隨機值（Base64Url），只存 SHA-256 雜湊於 `SysUserSession`；一次登入一筆 session。
- 以 HttpOnly Cookie `TcmClinic.Api.Refresh` 傳遞：非 Development 環境 `Secure`、`SameSite=Lax`（沿用原登入 Cookie 的設定）、`Path=/`，到期時間同 session。
- `POST /auth/refresh` 每次使用即輪替：產生新值、舊值記為「前一個」，session 到期時間延長為現在＋8 小時（滑動）。
- **重用偵測**：出示的是已輪替掉的前一個值，且距輪替超過 30 秒 → 視為外洩，撤銷整個 session。30 秒內視為多個分頁同時換新，只發新的 access token、不再輪替。
- 換新時同樣檢查帳號存在且有角色，否則撤銷 session。

**登出**

- `POST /auth/logout` 允許匿名、冪等：撤銷 refresh Cookie 對應的 session（有帶有效 access token 時也撤銷其 `sid`），清除 Cookie，回 204。

**資料表 `SysUserSession`**

- 系統層技術表：`Id`（IDENTITY）、`PublicId`（UUIDv7，即 `sid`）、`UserId`（外鍵 `SysUser`）、`TokenHash`、`PreviousTokenHash`、`ExpiresAt`、`LastRotatedAt`、`RevokedAt`、`RevokeReason` 與五個標準欄位。
- 標記 `[NotAudited]`：每 15 分鐘輪替一次，寫入資料異動紀錄只會產生大量無意義的雜湊變更；登入、登出等事件由日誌記錄。

## 影響（Consequences）

- 前端改為：登入取得 `accessToken` 存在記憶體、每次請求帶 `Authorization: Bearer`；收到 401 時呼叫 `POST /auth/refresh`（瀏覽器自動帶 refresh Cookie）換新再重試；重新整理頁面時先呼叫 refresh。前端由使用者另行說明。
- 每個請求多一次 session 查詢（與既有的角色查詢同一次往返之外再加一次）；目前規模可接受，熱點時可短暫快取。
- `SysUserSession` 筆數隨登入次數成長（每次登入一筆），不另做清除；量大時再規劃。
- 多實例部署時金鑰必須一致（設定相同的 `Jwt:SigningKey`）；session 存在資料庫，不受實例影響。
- 新增套件 `Microsoft.AspNetCore.Authentication.JwtBearer`（MIT）。
