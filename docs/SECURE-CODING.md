# 安全編碼規範（Secure Coding）

> 版本 1.3.0｜最後更新 2026-10-08

> 本檔為憲章原則 I（安全性 > 一切）的 **How**。目標：讓弱點掃描成為程式碼本已通過的閘門，而非事後補救。

## 1. 威脅基線

- OWASP Top 10 為最低威脅基線（注入、權限失控、加密失效、SSRF、不安全反序列化等）。

## 2. 注入與資料存取（MUST）

- 資料存取一律用參數化查詢／EF Core LINQ。**字串拼接 SQL 禁止**。
- 不得已用 raw SQL 時，必須用參數化 API（`FromSqlInterpolated` / 參數物件），絕不把使用者輸入內插進去。
- 同規則適用所有直譯器邊界（shell、檔案路徑、LDAP、以使用者輸入組成的 regex）：指令不得以拼接不可信輸入組成。

## 3. 輸入驗證（MUST）

- 所有外部輸入必須在 API 邊界驗證（強化憲章原則所述之分層驗證）；驗證必須是拒絕，而非默默修正畸形輸入。
- 回傳使用者可控內容時，必須做適當輸出編碼，避免注入；不得回傳未消毒的 raw HTML/腳本片段。

### 分層驗證（Defense in Depth）

| 層 | 手段 |
|----|------|
| API | FluentValidation 驗證 Request DTO（透過 FastEndpoints） |
| Use Case | 對命令/查詢做防禦性驗證，確保脫離 HTTP 情境仍健壯 |
| 基礎設施/服務邊界 | `Ardalis.GuardClauses`（`Guard.Against.Null` 等）fail-fast |
| 領域 | 以封裝強制業務不變式；假設輸入已驗證，違反則擲例外 |

## 4. 認證與授權（MUST）

- 存取控制必須 deny-by-default：每個非公開端點必須明確宣告其授權需求；只有刻意 opt-in（如 `AllowAnonymous()` 且附理由）才是公開。
- 授權必須在伺服器端強制。用戶端（UI）隱藏控制項不是存取控制手段。
- 必須做物件層級存取檢查——使用者不得靠猜 id 觸及他人資源（防 IDOR）。

## 5. 秘密管理（MUST）

- 秘密（連線字串、API key、token、簽章金鑰）禁止硬編碼於原始碼、提交的設定檔或任何用戶端 bundle。必須來自設定／環境變數／秘密儲存。

## 6. 加密（MUST）

- 傳輸中資料必須用 TLS/HTTPS。開發環境 HTTPS 重導的例外僅限本機 Aspire DCP proxy，非生產許可。
- 正式環境必須啟用 HTTPS 重導與 HSTS，並回應安全標頭：`X-Content-Type-Options: nosniff`、`X-Frame-Options: DENY`（或 CSP `frame-ancestors 'none'`）。由程式（`UseHttpsRedirection`／`UseHsts`／中介層）或 IIS 擇一負責，並在 `docs/INFRASTRUCTURE.md` 記錄由哪一層負責；目前尚未決定也未實作（specs/README.md 延後事項 DEF-006）。
- 密碼必須以現代加鹽雜湊儲存（如 ASP.NET Core Identity 預設、PBKDF2）；明文、未加鹽、快速雜湊儲存禁止。自製加密演算法禁止——使用平台已驗證的原語。
  - 實作為 `Pbkdf2PasswordHasher`：PBKDF2-HMAC-SHA512、100,000 次迭代（與 ASP.NET Core Identity 預設相同），以 BCL `Rfc2898DeriveBytes` 實作。2026-10-08 前 SPEC-004 依使用者需求採加鹽 SHA-256 的例外已撤銷；既有 SHA-256 雜湊於登入成功時自動轉換，且不變更密碼修改時間（SPEC-004 FR-007a、[ADR-009](../specs/其他/使用者管理與登入/ADR-009-password-hashing-pbkdf2.md)）。
- 密碼強度規則以 Core `PasswordPolicy` 為全系統唯一來源（12～128 碼且同時含大寫、小寫、數字）；任何設定密碼的入口必須經由它（Web 層用 `MustSatisfyPasswordPolicy()`），不得自行撰寫規則，避免各入口規則漂移（個人資料維護 FR-008a）。

## 7. 日誌中的秘密與個資（MUST）

- 日誌不得含秘密或敏感資料——密碼、雜湊、token、cookie、完整連線字串、個資（PII）必須排除或遮罩後才記錄。
- 用結構化日誌（Serilog message template），不要預先內插字串。
- 記錄關聯/追蹤 id 以便分散式追蹤。
- 語意化 log level：`Information` 正常生命週期、`Warning` 可復原異常、`Error` 已處理失敗（附例外物件）、`Fatal` 終止程序等級。預期的 `Result` 失敗（`NotFound`/`Invalid`）不得記為 `Error`。

## 8. 掃描閘門（MUST）

- **秘密掃描在每次 commit 前執行**（2026-10-08 裁決，對齊 UDS）：以 git pre-commit hook 執行 gitleaks（檢查暫存變更是否夾帶金鑰、密碼、連線字串），掃到就擋下 commit；AI 自動 commit 同樣經過此 hook。理由：分支會 push 到 GitHub，合併前才掃時秘密可能已進入遠端歷史。hook 為進版控的 `.githooks/pre-commit`（啟用方式見 docs/INFRASTRUCTURE.md），找不到 gitleaks 時同樣擋下 commit。
  - 誤判的放行寫在 `.gitleaks.toml`，每條須附理由與追溯編號，且只針對特定規則與路徑（目前只有「測試專案的 generic-api-key」一條）。
- 合併前，作者必須對受影響堆疊執行本機/開發階段安全掃描：相依套件弱點掃描（`dotnet list package --vulnerable --include-transitive`）+ 變更程式碼的 SAST（2026-10-08 裁決維持合併前執行，不提前到 commit 或 push）。
- **High/Critical** 發現必須修復（或升級/替換相依）才能合併；Code Review 為此閘門的執行點（直到 CI 自動化建立）。Medium/Low 須記錄並可排期。
- 任何抑制/基線忽略必須附書面理由與可追溯單號，禁止無聲抑制。

## 9. 執行矩陣

| 關注點 | 規則 | 嚴格度 |
|--------|------|--------|
| SQL/直譯器注入 | 僅參數化；禁止字串拼接 | MUST |
| 輸出編碼 | 回傳使用者可控內容須適當編碼；不回未消毒 raw HTML | MUST |
| 授權 | deny-by-default、逐端點明確、伺服器端、物件層級 | MUST |
| 秘密 | 不進原始碼/提交設定/用戶端 bundle | MUST |
| 加密 | 傳輸 TLS；加鹽現代雜湊（PBKDF2，ADR-009）；不自製加密 | MUST |
| 相依弱點 | 開發階段掃描；High/Critical 擋合併 | MUST |
| SAST | 開發階段掃描變更碼；High/Critical 擋合併 | MUST |
| 秘密掃描 | 每次 commit 前以 pre-commit hook（`.githooks/pre-commit`）掃描暫存變更；偵測到即擋 commit | MUST |
| 掃描抑制 | 須理由 + 單號 | MUST |
| HTTPS 重導、HSTS、安全標頭 | 正式環境必須具備；程式或 IIS 擇一負責（DEF-006） | MUST |

## 10. 不採用或暫不適用的項目

下列為 UDS 安全標準要求、但本專案經裁決不採用或暫不適用者（規格未要求的功能不新增）：

| 項目 | 本專案做法 | 理由 |
|------|-----------|------|
| MFA | 不提供 | 規格未要求；內部後台 |
| API 速率限制 | 登入失敗鎖定（同帳號 5 次鎖 15 分鐘，SPEC-004 FR-006a）＋登入端點依來源 IP 限速（每分鐘 20 次、超過回 429 附 Retry-After，FR-006b）；不做全站 IP 層節流 | 2026-10-08 使用者決定登入端點加限速；其他端點不限速 |
| 完整 CSP 標頭 | 只要求 `X-Frame-Options: DENY` 或 CSP `frame-ancestors`（§6） | 純 JSON API 不輸出 HTML，完整 CSP 效益低 |
| 密碼雜湊演算法 | PBKDF2（Identity 預設）即可，不限定 Argon2id／bcrypt／scrypt | PBKDF2 為 NIST 認可演算法 |
| SAST、相依套件掃描時點 | 合併前執行（§8），不提前到 commit 或 push | 合併前即可攔下；每次 commit 執行會拖慢流程 |
| 登入工作階段 | JWT Bearer：access token 15 分鐘（前端只存記憶體）；refresh token 以 HttpOnly Cookie 傳遞（`SameSite=Lax`、非 Development `Secure`）、8 小時滑動、每次換新輪替、重用偵測、伺服器只存雜湊；簽章金鑰 `Jwt:SigningKey` 由環境提供 | 2026-10-08 使用者決定（SPEC-004 FR-021～FR-021c、ADR-011） |
| DAST | 暫不執行 | 尚無 staging 環境 |
| 秘密輪替週期、Vault | 不強制 | 客戶自建環境，由部署端管理設定與環境變數 |
