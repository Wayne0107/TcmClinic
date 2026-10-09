# [SPEC-004] Feature: 使用者管理與登入

<!-- spec-type: feature -->

| 項目 | 內容 |
|------|------|
| SPEC-ID | SPEC-004 |
| 規格類型 | feature |
| 狀態 | Archived |
| 選單 | 無（跨選單基礎規格）；使用者管理 5 支端點自 2026-10-02 起由「管理業務 › 系統管理作業 › 帳號維護」（`accountMaintenance`）授權；登入頁路由 `/login` |
| 建立日期 | 2026-08-28 |
| 核准日期 | 2026-10-08（需求變更：密碼雜湊改 PBKDF2；登入端點限速；登入改 JWT） |
| 核准人 | Wayne（授權 AI 直接決定並修改） |
| 最後更新 | 2026-10-08（需求變更：FR-007 密碼雜湊改 PBKDF2、新增 FR-007a；新增 FR-006b 登入限速；登入改 JWT Bearer＋refresh token（FR-002、FR-005、FR-021 修改，新增 FR-021a～c，ADR-011）；見「變更（Delta）」） |
| 原分支 | `004-user-management`；2026-10-08 需求變更 `feature/004-pbkdf2-password`、`feature/004-login-rate-limit`、`feature/004-jwt-auth` |
| 相關規格 | 依賴 [SPEC-002 後台選單與角色權限管理](../後台選單與角色權限管理/spec.md)（角色、角色↔選單可見性）；⚠️ FR-018/019/022 被 [SPEC-005 選單導向授權模型](../選單導向授權模型/spec.md) 取代；⚠️ FR-013 密碼規則被 [SPEC-013 個人資料維護](../../管理業務/系統管理作業/個人資料維護/spec.md) FR-008a 修改（另擴充 `SysUser` 電話／Email／密碼修改時間、`BranchId` 外鍵）；⚠️ 使用者清單預設每頁筆數被 SPEC-016 系統參數維護 FR-008(b) 修改；SPEC-011 受理開狀申請書 擴充 `SysUser`（機構）；SPEC-007 CHB選單與角色種子（`accountMaintenance` 選單）；[SPEC-008 選單API欄位對齊前端](../選單API欄位對齊前端/spec.md)（`/me/menus` 回應欄位）；[SPEC-009 開發模式可見全部選單](../開發模式可見全部選單/spec.md)（目前僅開發模式 SUPERADMIN 可見帳號維護）；SPEC-058 [帳號維護](../../管理業務/系統管理作業/帳號維護/spec.md)（決定 `accountMaintenance` 初始可見角色） |

> 📦 本規格隨範本沿用自源專案（CHB），已實作於骨架。內文提到的分行、企業、開狀申請、系統參數、CHB 選單／角色等屬源專案業務，範本已移除；以骨架程式碼與測試為準。

> **原狀態紀錄**：Implemented（2026-08-28）；2026-09-30 修訂已完成（資料表規範對齊，見 Clarifications Session 2026-09-30）；2026-10-02 移除舊後台選單（見 Clarifications Session 2026-10-02）。
>
> ⚠️ **被後續規格修改**：
> - FR-018／FR-019／FR-022（超管守門、防鎖死、超管解救）已由 SPEC-005 整組取代；US2～US5 敘述中的「超級管理員」及 US4 情境 3（引用防鎖死部分）、US5 情境 2、SC-007 保留為歷史紀錄。
> - FR-013 密碼強度規則自 2026-09-29 起由 SPEC-013 FR-008a 改為 12～128 碼且同時包含大寫、小寫、數字。
> - 使用者清單 `pageSize` 預設值改取 SPEC-016「查詢列表分頁筆數」（初始 10；2026-10-02 前預設 20）。⚠️ 2026-10-06 SPEC-016 移除該參數，預設改為固定 10（值不變，[ADR-007](../架構決策/ADR-007-default-page-size-fixed.md)）；API 契約內文於 SPEC-016 實作時一併更新。
> - 2026-10-02 起舊後台選單 `/admin/users` 移除，使用者管理改由 `accountMaintenance` 授權。
> - 2026-10-08 起 FR-007 密碼雜湊由加鹽 SHA-256 改為 PBKDF2，並新增 FR-007a 登入時自動轉換既有雜湊；設計決策 R2 由 [ADR-009](ADR-009-password-hashing-pbkdf2.md) 取代（見「變更（Delta）2026-10-08」）。文中其他提到 SHA-256 之處保留為歷史紀錄。
> - 2026-10-08 起新增 FR-006b：同一來源 IP 每分鐘超過 20 次登入請求回 429（延後事項 DEF-017）。
> - 2026-10-08 起登入改用 JWT：access token（15 分鐘）以 `Authorization: Bearer` 傳遞，refresh token（8 小時滑動、可撤銷）以 HttpOnly Cookie 傳遞；設計決策 R1（Cookie 驗證）由 [ADR-011](ADR-011-jwt-bearer-authentication.md) 取代，R8 每請求重驗改在 JWT 驗證後執行。
> - 2026-10-08 起使用者 API 的識別碼（路徑 `{id}`、回應 `id`、請求 `roleIds`）改為 UUIDv7 對外識別碼，不再公開整數主鍵（[SPEC-064](../對外識別碼UUIDv7/spec.md)）。

## Overview（概述）

建立後台帳號系統：資料表 `SysUser`（帳號、加鹽 SHA-256 密碼、使用者名稱）與使用者↔角色多對多關聯 `SysUserRole`（原 `SysUserRoles`）。網站入口為登入頁，使用者以帳號密碼登入後依所屬角色取得對應選單；並提供使用者的新增、查詢、修改（含重設密碼、變更角色）、刪除（軟刪除）。本功能補齊 SPEC-002 刻意延後的「使用者↔角色指派」與登入機制，以 ASP.NET Core Cookie 驗證讓既有授權政策於正式環境生效。

原始需求：「接下來幫我建立使用者管理，資料表用SysUser，有帳號，密碼，密碼用SHA256去加密，還有加salt，還有使用者名稱，還有對應SysRoles，目前就這些欄位，然後網站一進去就是登入頁，就是根據帳號密碼去登入，登入後就可以得到相對應選單之類的，功能是使用者管理，可以新增刪除修改查詢這些」

## Motivation（動機）

本功能是既有後台帳號系統的建立：SPEC-002 負責「角色可見哪些選單」，本功能負責「使用者是誰、屬於哪些角色、如何登入」。在此之前，002/003 端點雖以 `SuperAdminPolicy`（`RequireRole`）保護，但正式環境尚未接任何驗證 scheme（測試以 `TestAuthHandler` 代打）。

- **US1（P1）登入**：登入是整個系統的入口與安全門檻；沒有登入與身分建立，後續的使用者管理與選單授權都無對象可作用。以一個預先種入的超級管理員帳號即可構成最小可行切片。
- **US2（P2）查詢**：查詢是管理其他操作（新增前確認是否重複、修改/刪除前定位對象）的基礎；依賴 US1 已可登入。
- **US3（P2）新增**：建立新帳號是使用者管理的核心寫入操作，也讓 002 的角色可見性能夠套用到真實使用者；依賴 US1、US2。
- **US4（P3）修改**：支援日常維運（職務異動改角色、忘記密碼重設）；在 US1–US3 完成後即可運作，故列 P3。
- **US5（P3）刪除**：帳號生命週期的收尾操作；相依於前述故事，且刪除頻率低，列 P3。

## Clarifications（釐清紀錄）

### Session 2026-08-28

- Q: 帳號（登入識別碼）的唯一性與登入比對是否區分大小寫？ → A: 不分大小寫；帳號正規化為小寫後再進行唯一性比對與登入查找。
- Q: 是否強制每位使用者至少擁有一個角色？ → A: 是；新增／修改時必填至少一個角色，無角色不可儲存（取代原自相矛盾的 FR-016）。
- Q: 密碼的最小強度規則為何？ → A: 最少 8 碼，且須同時包含英文字母與數字。
- Q: 連續登入失敗時是否納入帳號鎖定？ → A: 納入基本鎖定——同一帳號連續 5 次登入失敗後鎖定約 15 分鐘，逾時自動解除。

### Session 2026-09-30

背景：DATABASE-SCHEMA §8 要求資料表名一律單數；使用者角色關聯表 `SysUserRoles` 為複數，所參照的角色表也由「後台選單與角色權限管理」改為 `SysRole`。

- Q: 使用者角色關聯表名是否改為單數？ → A: **改**：`SysUserRoles`→`SysUserRole`，既有資料保留；角色表隨 002 改為 `SysRole`（原 `SysRoles`）。取代 2026-08 沿用指定表名的做法。外部可觀察行為不變。

### Session 2026-10-02

背景：CHB 選單已併入規格（見 CHB選單與角色種子），範本早期的舊後台選單（`/admin/users`、`/admin/roles`、`/admin/audit-logs`、`/admin/access-logs`）不再使用。使用者決定直接移除並修訂相關規格，不另開規格。

- Q: 舊後台選單 `/admin/users` 移除後，使用者管理改由哪個選單授權？ → A: 使用者管理 5 支端點（查詢、新增、修改、刪除、重設密碼）改由 CHB「帳號維護」選單（`accountMaintenance`）授權；具 `/admin/users` 可見性者不再能存取。哪些角色初始可見「帳號維護」，留待 SPEC-058 [帳號維護](../../管理業務/系統管理作業/帳號維護/spec.md) 規格決定；目前僅開發模式的 SUPERADMIN 可見（[開發模式可見全部選單](../開發模式可見全部選單/spec.md)）。

### Session 2026-10-08（密碼雜湊改 PBKDF2）

背景：FR-007 原依使用者明示需求採加鹽 SHA-256，並登記為已接受風險（DEF-008）。2026-10-08 開發規範裁決（PRJ-2026-0008）撤銷此例外，改用專案規範允許的 PBKDF2。使用者授權由 AI 直接決定以下細節並修改程式與規格。

- Q: 改用哪個演算法與參數？ → A: **PBKDF2-HMAC-SHA512、100,000 次迭代、16-byte 隨機 salt、32-byte 輸出**，與 ASP.NET Core Identity v3 預設相同；以 .NET 內建 `Rfc2898DeriveBytes.Pbkdf2` 實作，不新增套件（[ADR-009](ADR-009-password-hashing-pbkdf2.md)）。
- Q: 系統中既有的 SHA-256 雜湊如何轉換？ → A: **登入成功時自動重新雜湊**，不要求使用者重設密碼；轉換前舊雜湊仍可驗證。
- Q: 自動重新雜湊是否重置密碼效期？ → A: **不重置**。只換雜湊與 salt，`PasswordChangedAt` 不變；使用者沒有真的改密碼，SPEC-013 FR-010/011 的效期不得因此重來。
- Q: 如何分辨新舊雜湊？ → A: 新格式為 `pbkdf2-sha512$<迭代次數>$<Base64 雜湊>`（65 字元，沿用 `nvarchar(88)`）；沒有此前綴者視為舊 SHA-256。迭代次數低於現行值者，同樣在登入時重新雜湊。資料表結構不變。

### Session 2026-10-08（登入端點限速）

背景：2026-10-08 開發規範裁決第 36 題（速率限制）列為待討論；使用者決定登入端點要加速率限制（DEF-017），全站 `X-RateLimit-*` 標頭仍不做。

- Q: 以什麼為單位、上限多少？ → A: **每個來源 IP 每分鐘 20 次**（使用者選定）。分行多名行員可能共用同一個對外 IP，較嚴的「15 分鐘 5 次」容易在上班尖峰互相擋住；同一帳號的暴力破解另由 FR-006a 帳號鎖定處理，本條主要擋大量撞庫與跨帳號密碼噴灑。上限與時間窗可在設定檔 `LoginRateLimit` 調整。
- Q: 超過上限怎麼回應？ → A: **429 ProblemDetails，附 `Retry-After`（建議等待秒數）**；請求不進入登入驗證，因此不計入帳號連續失敗次數，也不會因此觸發帳號鎖定。
- Q: 計數方式？ → A: 固定時間窗、行程內計數（與 FR-006a 鎖定一樣，重啟後歸零）；只套用於 `POST /auth/login`，不做全站 IP 節流（`docs/SECURE-CODING.md`）。被擋下時記一筆 `Warning`（`[LOGIN_RATE_LIMITED]`，含來源 IP，不記帳號與密碼）。

### Session 2026-10-08（登入改 JWT）

背景：使用者決定登入改用 JWT。token 傳遞方式與效期續期方式由使用者選定，其餘細節授權 AI 決定（[ADR-011](ADR-011-jwt-bearer-authentication.md)）。

- Q: token 怎麼交給前端、前端怎麼保存？ → A: **Bearer 標頭＋前端存記憶體**（使用者選定）。登入回傳 access token，前端放記憶體、每次請求帶 `Authorization: Bearer`；重新整理頁面時以 HttpOnly Cookie 中的 refresh token 換新。XSS 讀不到長效憑證，維持 FR-021 的精神。
- Q: 效期與續期？ → A: **access token 15 分鐘＋refresh token 8 小時滑動**（使用者選定）。refresh token 存資料庫（只存雜湊）、每次使用即輪替、登出可撤銷，對應原本 Cookie 的 8 小時滑動逾時。
- Q: 權限變更、刪除帳號、登出要多快生效？ → A: **下一個請求即生效**，與原 R8 相同。角色與可見選單不放進 token，每個請求照舊查資料庫；另檢查 token 所屬 session 未撤銷、未逾期。
- Q: refresh token 外洩怎麼辦？ → A: **重用偵測**：出示已輪替掉的前一個值且距輪替超過 30 秒，撤銷整個 session；30 秒內視為多分頁同時換新，只發新的 access token。
- Q: 登出需要已登入嗎？ → A: **不需要、冪等**。access token 過期時使用者仍要能登出；有 refresh Cookie 就撤銷對應 session，一律清除 Cookie 並回 204。
- Q: 簽章金鑰？ → A: 設定 `Jwt:SigningKey`（Base64，至少 32 bytes），非 Development 未設定即無法啟動；Development 未設定時啟動產生隨機金鑰，不把金鑰寫進版控。

## 變更（Delta）

### 2026-10-08 登入改 JWT

依 Clarifications Session 2026-10-08（登入改 JWT）與 [ADR-011](ADR-011-jwt-bearer-authentication.md)。

**MODIFIED Requirements**

- FR-002：登入成功後建立的工作階段改為「session＋access token＋refresh token」。
- FR-005：登出改為撤銷 session、清除 refresh Cookie，允許匿名且冪等；之後該 session 的 access token 與 refresh token 一律 401。
- FR-021：由「以 HttpOnly Cookie 承載登入工作階段」改為「access token 以 Bearer 傳遞、前端只存記憶體；refresh token 只以 HttpOnly Cookie 傳遞、伺服器只存雜湊」。
- AC-2、AC-5、AC-24 改寫；邊界情境「工作階段逾期」改寫。
- API 契約：`POST /auth/login` 回應改為含 `accessToken`、`tokenType`、`expiresIn`，並以 `Set-Cookie` 設定 refresh Cookie；`POST /auth/logout` 改為匿名；新增 `POST /auth/refresh`。
- 設計決策 R1 由 ADR-011 取代；R8 改在 JWT 驗證後（`JwtBearerEvents.OnTokenValidated`）執行，行為不變。

**ADDED Requirements**

- FR-021a：refresh 換發與輪替、8 小時滑動。
- FR-021b：refresh token 重用偵測（30 秒寬限）。
- FR-021c：每個請求檢查 session 有效。
- AC-33～AC-41（見驗收條件表）。
- 資料表 `SysUserSession`（見 ADR-011）。

**連帶影響**

- 前端：改用 Bearer＋refresh 流程（見 ADR-011「影響」），由使用者向前端團隊說明。
- `AGENTS.md` 已裁決表「登入 Cookie」列改為 JWT；新增套件 `Microsoft.AspNetCore.Authentication.JwtBearer`（`THIRD-PARTY-NOTICES.md`）。

### 2026-10-08 登入端點限速

依 Clarifications Session 2026-10-08（登入端點限速）。

**ADDED Requirements**

- FR-006b：同一來源 IP 在 1 分鐘內對 `POST /auth/login` 的請求超過 20 次（可設定）時，系統 MUST 回 429 ProblemDetails 並附 `Retry-After`；被拒的請求 MUST NOT 進入登入驗證、MUST NOT 計入帳號連續失敗次數。
- AC-32：同一 IP 超過上限回 429 且附 `Retry-After`；不同 IP 分開計算；被拒的請求不累計帳號失敗次數。

**MODIFIED Requirements**

- FR-006a：移除「是否另加速率限制待討論」的註記，改指向 FR-006b。
- API 契約 `POST /auth/login` 增列 429 回應。

**連帶影響**

- 延後事項 DEF-017 完成；`AGENTS.md` 已裁決表、`docs/SECURE-CODING.md`、`docs/API-CONVENTIONS.md`、`docs/LOGGING.md` 同步。
- API 契約：新增 429 回應，前端需處理（顯示「請稍後再試」）。

### 2026-10-08 密碼雜湊改 PBKDF2

依 Clarifications Session 2026-10-08。原文已就地修改並標註日期；本節列出變更清單供審查與追溯。

**MODIFIED Requirements**

- FR-007：由「加鹽之單向雜湊（SHA-256）」改為「加鹽之 PBKDF2（HMAC-SHA512、100,000 次迭代）」。
- AC-13：驗證對象改為 `Pbkdf2PasswordHasher`，並涵蓋新格式字串與舊 SHA-256 雜湊仍可驗證。
- 資料模型 `PasswordHash` 欄位說明改為新格式（欄位型別不變）。
- 設計決策 R2 由 [ADR-009](ADR-009-password-hashing-pbkdf2.md) 取代；原則符合性與 Risks 中「SHA-256 為已知取捨」兩項標為不再適用。

**ADDED Requirements**

- FR-007a：登入驗證成功時，若儲存的雜湊為舊格式或迭代次數低於現行值，系統 MUST 以現行參數重新雜湊並保存，且 MUST NOT 變更密碼修改時間。
- AC-31：以舊 SHA-256 雜湊建立的帳號，登入成功後雜湊改為新格式、密碼修改時間不變，之後仍可用同一密碼登入。

**連帶影響**

- 慣例文件：`docs/SECURE-CODING.md` §6、§9 的「現況仍為 SHA-256」註記移除。
- 延後事項 DEF-008 完成。
- 前端與 API 契約不受影響。


## Requirements（需求）

### Requirement: US1 以帳號密碼登入並取得對應選單（P1）

The system SHALL 讓未登入的存取者一進入網站即看到登入頁，以帳號＋密碼登入成功後建立登入工作階段，並依其所屬角色顯示對應的側邊導覽選單（多角色為聯集，沿用 002 的角色↔選單可見性規則）。

驗證失敗則停留在登入頁並顯示錯誤訊息。獨立驗證方式：種入一個超級管理員帳號後，以正確帳密登入應進入後台並看到其角色對應之選單；以錯誤帳密登入應被拒絕。涵蓋 FR-001～FR-006a、FR-021。

#### Scenario: AC-1 未登入導向登入頁
- **GIVEN** 未登入的存取者開啟網站
- **WHEN** 進入任一頁面
- **THEN** 系統導向登入頁

#### Scenario: AC-2 正確帳密登入成功
- **GIVEN** 登入頁
- **WHEN** 使用者輸入正確的帳號與密碼並送出
- **THEN** 系統驗證通過、建立登入狀態並進入後台首頁

#### Scenario: AC-3 錯誤帳密以一般性訊息拒絕
- **GIVEN** 登入頁
- **WHEN** 使用者輸入不存在的帳號或錯誤密碼
- **THEN** 系統拒絕登入並顯示一般性錯誤訊息（不透露是帳號或密碼何者有誤）

#### Scenario: AC-4 側邊選單為所屬角色可見選單聯集
- **GIVEN** 已成功登入的使用者
- **WHEN** 進入後台
- **THEN** 側邊選單僅顯示其所屬角色被授權可見的選單項目（多角色時為聯集）

#### Scenario: AC-5 登出清除登入狀態
- **GIVEN** 已登入的使用者
- **WHEN** 執行登出
- **THEN** 登入狀態被清除，再次存取受保護頁面會被導回登入頁

#### Scenario: AC-6 軟刪除或無角色帳號不可登入
- **GIVEN** 已被軟刪除或無任何角色的帳號
- **WHEN** 嘗試登入
- **THEN** 系統拒絕登入（軟刪除帳號不可登入）

### Requirement: US2 查詢與檢視使用者清單（P2）

The system SHALL 提供使用者分頁清單，支援依帳號或使用者名稱關鍵字搜尋，顯示帳號、使用者名稱與所屬角色，且不顯示任何密碼資訊。

超級管理員在「使用者管理頁」檢視所有使用者。獨立驗證方式：在已有數筆使用者資料的前提下，以超級管理員登入後開啟使用者管理頁，輸入關鍵字搜尋應回傳符合的使用者且分頁正確，畫面不出現密碼或雜湊值。涵蓋 FR-009、FR-010。

#### Scenario: AC-7 分頁清單顯示未刪除使用者及角色
- **GIVEN** 使用者管理頁
- **WHEN** 超級管理員開啟頁面
- **THEN** 系統以分頁清單顯示所有未被刪除的使用者及其所屬角色

#### Scenario: AC-8 關鍵字搜尋
- **GIVEN** 使用者清單
- **WHEN** 輸入帳號或使用者名稱關鍵字搜尋
- **THEN** 僅顯示符合關鍵字的使用者

#### Scenario: AC-9 清單不顯示密碼資訊
- **GIVEN** 使用者清單的任一列
- **WHEN** 檢視其內容
- **THEN** 顯示帳號、使用者名稱與角色，且不顯示密碼、密碼雜湊或 salt

### Requirement: US3 新增使用者（P2）

The system SHALL 允許新增使用者（帳號、密碼、使用者名稱，並指派一或多個角色），以加鹽的單向雜湊保存密碼，且帳號不可與現有未刪除帳號重複。

獨立驗證方式：以超級管理員登入後新增一名使用者並指派角色，儲存後應出現在清單中，且該使用者能以所設定的帳密登入並看到其角色對應之選單。涵蓋 FR-007、FR-008、FR-011～FR-013。

#### Scenario: AC-10 新增使用者成功
- **GIVEN** 新增使用者表單
- **WHEN** 填入帳號、密碼、使用者名稱並指派至少一個角色後儲存
- **THEN** 系統建立該使用者並顯示於清單

#### Scenario: AC-11 帳號重複被阻擋
- **GIVEN** 新增使用者表單
- **WHEN** 填入的帳號與現有（未刪除）帳號重複
- **THEN** 系統阻擋建立並提示帳號已存在

#### Scenario: AC-12 必填欄位驗證
- **GIVEN** 新增使用者表單
- **WHEN** 未填必要欄位（帳號、密碼或使用者名稱）
- **THEN** 系統阻擋建立並顯示欄位層級的驗證訊息

#### Scenario: AC-13 密碼以加鹽單向雜湊保存
- **GIVEN** 已新增的使用者
- **WHEN** 檢視其保存的密碼資料
- **THEN** 系統儲存的是加鹽的單向雜湊，而非可還原的明文

### Requirement: US4 修改使用者（含重設密碼與變更角色）（P3）

The system SHALL 允許修改使用者名稱、重新指派角色與重設密碼，帳號（登入識別碼）不可修改，且角色變更於該使用者下次載入頁面即生效。

獨立驗證方式：以超級管理員登入後編輯某使用者的名稱與角色並儲存，重新讀取應反映變更；重設密碼後，該使用者僅能以新密碼登入、舊密碼失效。涵蓋 FR-014～FR-016。

#### Scenario: AC-14 修改名稱或角色並即時反映
- **GIVEN** 編輯使用者表單
- **WHEN** 修改使用者名稱或所屬角色並儲存
- **THEN** 變更保存且清單與後續登入之選單反映最新角色

#### Scenario: AC-15 重設密碼後舊密碼失效
- **GIVEN** 編輯使用者表單
- **WHEN** 執行重設密碼並輸入新密碼
- **THEN** 系統以新密碼的加鹽雜湊覆寫，舊密碼即失效

#### Scenario: AC-16 至少保留一個角色
- **GIVEN** 一名使用者的所有角色被移除
- **WHEN** 儲存
- **THEN** 系統阻擋儲存並提示每位使用者至少需保留一個角色（見 FR-016）

### Requirement: US5 刪除使用者（軟刪除）（P3）

The system SHALL 以軟刪除（保留紀錄）刪除使用者帳號；被刪除的帳號不再出現於清單、無法登入，且帳號名稱可被重用。

獨立驗證方式：以超級管理員登入後刪除某使用者，該使用者應自清單消失且無法再登入，但資料仍以軟刪除保留可供稽核。涵蓋 FR-017、FR-019（已被取代）。

#### Scenario: AC-17 軟刪除使用者
- **GIVEN** 使用者清單
- **WHEN** 超級管理員刪除某使用者
- **THEN** 該使用者被標記為已刪除、自清單消失且無法登入

#### Scenario: AC-18 防鎖死（已被 SPEC-005 取代）
- **GIVEN** 目前登入的超級管理員本人
- **WHEN** 嘗試刪除自己的帳號或移除自身超級管理員角色
- **THEN** 系統阻擋以避免鎖死（見 FR-017）

#### Scenario: AC-19 軟刪除後帳號名稱可重用
- **GIVEN** 一個已被軟刪除的帳號
- **WHEN** 之後以相同帳號名稱新增使用者
- **THEN** 系統允許重用該帳號名稱（唯一性僅約束未刪除的帳號）

### 功能需求條文（Functional Requirements）

**登入與工作階段（US1）**

- **FR-001**: 系統 MUST 以登入頁作為未登入存取者的入口；未登入而存取任何受保護頁面時 MUST 導向登入頁。
- **FR-002**: 系統 MUST 允許使用者以「帳號 + 密碼」登入，並在驗證成功後建立登入工作階段。（2026-10-08 修改：工作階段由 session、access token 與 refresh token 組成，見 FR-021～FR-021c。）
- **FR-003**: 登入驗證失敗時，系統 MUST 拒絕登入並回覆一般性錯誤訊息，MUST NOT 透露是帳號不存在或密碼錯誤（避免帳號列舉）。
- **FR-004**: 登入成功後，系統 MUST 依使用者所屬角色顯示對應的側邊導覽選單；多角色時為各角色可見選單的聯集（可見性規則沿用 002）。
- **FR-005**: 系統 MUST 提供登出功能。（2026-10-08 修改）登出 MUST 撤銷目前的 session 並清除 refresh Cookie；之後該 session 的 access token 與 refresh token MUST 一律回 401。登出 MUST 允許匿名且冪等（未登入或 token 已過期時亦回 204），前端登出後導回登入頁。
- **FR-006**: 被軟刪除（`Removed=1`）或無任何有效角色的帳號 MUST NOT 能登入取得後台存取權。
- **FR-006a**: 系統 MUST 對連續登入失敗實施基本帳號鎖定——同一帳號連續 5 次登入失敗後，MUST 於約 15 分鐘內拒絕該帳號的後續登入嘗試（鎖定期間即使帳密正確亦拒絕），逾時後 MUST 自動解除；鎖定回覆仍維持一般性訊息，MUST NOT 洩漏鎖定計數細節以外之帳號資訊。（依來源 IP 的登入限速見 FR-006b。）
- **FR-006b**（2026-10-08 新增）: 同一來源 IP 在 1 分鐘內對 `POST /auth/login` 的請求超過 20 次（設定檔 `LoginRateLimit` 可調整）時，系統 MUST 回 429 ProblemDetails 並附 `Retry-After`（建議等待秒數）；被拒的請求 MUST NOT 進入登入驗證、MUST NOT 計入 FR-006a 的帳號連續失敗次數。

**密碼安全（US1、US3、US4）**

- **FR-007**: 系統 MUST 以「加鹽（salt）之 PBKDF2（HMAC-SHA512、100,000 次迭代）」保存使用者密碼；MUST NOT 以明文或可還原形式儲存密碼。（2026-10-08 修改：原為加鹽 SHA-256，見「變更（Delta）2026-10-08」、[ADR-009](ADR-009-password-hashing-pbkdf2.md)）
- **FR-007a**（2026-10-08 新增）: 登入驗證成功時，若該帳號儲存的雜湊為舊格式（SHA-256）或迭代次數低於現行值，系統 MUST 以現行參數重新雜湊並保存；MUST NOT 因此變更密碼修改時間（SPEC-013 FR-010/011 的效期照舊計算）。
- **FR-008**: 每個帳號 MUST 具有各自獨立的 salt；密碼驗證 MUST 以輸入密碼結合該帳號 salt 計算雜湊後與儲存值比對。
- **FR-009**: 系統於任何清單、明細或紀錄中 MUST NOT 顯示或回傳密碼明文、密碼雜湊或 salt。

**使用者管理 — 查詢（US2）**

- **FR-010**: 系統 MUST 提供使用者清單，顯示帳號、使用者名稱與所屬角色，並支援關鍵字（帳號／使用者名稱）搜尋與分頁。清單 MUST 僅包含未被軟刪除的使用者。

**使用者管理 — 新增（US3）**

- **FR-011**: 系統 MUST 允許新增使用者，必填欄位為帳號、密碼、使用者名稱，並 MUST 指派至少一個角色（SysRoles，可多個）；無角色不可儲存。
- **FR-012**: 帳號在「未刪除的使用者」範圍內 MUST 唯一；比對前 MUST 對帳號進行正規化（去除前後空白並轉為小寫）。帳號的唯一性判定與登入查找皆 MUST 不分大小寫。
- **FR-013**: 新增時系統 MUST 於欄位層級驗證必填與格式；密碼 MUST 至少 8 碼、MUST NOT 超過 128 碼（防止過長輸入造成之資源濫用），且同時包含英文字母與數字。未通過時 MUST 阻擋建立並回覆欄位層級訊息。
  > ⚠️ **密碼強度規則已由「個人資料維護」FR-008a 修改（2026-09-29）**：全系統統一為 12～128 碼，且同時包含大寫字母、小寫字母、數字；適用新增、重設與個人變更密碼，既有密碼不追溯。見 [SPEC-013 個人資料維護](../../管理業務/系統管理作業/個人資料維護/spec.md)。

**使用者管理 — 修改（US4）**

- **FR-014**: 系統 MUST 允許修改使用者名稱與重新指派角色，並 MUST 提供重設密碼功能；重設的新密碼 MUST 同樣符合密碼強度規則（見 FR-013），重設後 MUST 以新密碼的加鹽雜湊覆寫、舊密碼立即失效。帳號（登入識別碼）預設 MUST NOT 可修改（避免識別碼漂移；如需更名採停用後新建）。
- **FR-015**: 修改角色後，系統 MUST 於該使用者下次載入頁面時套用最新的角色與對應可見選單。
- **FR-016**: 每位使用者 MUST 至少保留一個角色；修改時若移除全部角色，系統 MUST 阻擋儲存並提示至少需保留一個角色。

**使用者管理 — 刪除（US5）**

- **FR-017**: 系統 MUST 支援使用者的軟刪除（`Removed=1`）；被刪除者 MUST 自清單消失且 MUST NOT 能登入。硬刪除 PROHIBITED（除非另有法遵抹除需求）。

**存取控制與防鎖死（共通）**

> ⚠️ **本區塊（FR-018/019/022）已由 005（選單導向授權）整組取代**：授權改為「對該頁選單有可見性者即可存取其寫入功能」；移除超級管理員守門與所有防鎖死/解救機制，鎖死改由人工修改資料表恢復。見 [SPEC-005 選單導向授權模型](../選單導向授權模型/spec.md)。以下 FR 保留為歷史紀錄。

- **FR-018**: 使用者管理頁 MUST 僅限「超級管理員」角色存取（與 002 FR-017 一致）。
- **FR-019**: 系統 MUST 阻擋任何會使系統失去唯一超級管理員的操作——包含刪除最後一位超級管理員、或移除當前登入者自身之超級管理員角色（防鎖死）。
- **FR-022**: 系統 MUST 保有至少一條「超級管理員無法登入時」之解救途徑：唯一超管因連續登入失敗遭鎖定時，鎖定為非持久化之行程內狀態，重啟服務即自動解除；初始超管帳號 MUST 由種子（SeedData）冪等建立，於帳號遺失或無法登入時可重新種入以恢復管理入口。

**傳輸與跨站安全（共通）**

- **FR-021**（2026-10-08 改寫，原「以 HttpOnly Cookie 承載登入工作階段」）: 登入成功 MUST 回傳 access token（JWT，效期 15 分鐘），呼叫端以 `Authorization: Bearer` 傳遞；前端 MUST 只把 access token 存在記憶體，MUST NOT 存於 localStorage 等 JavaScript 可長期讀取之處。refresh token MUST 只以 HttpOnly Cookie 傳遞（非 Development 環境 `Secure`、`SameSite=Lax`、`Path=/`），伺服器 MUST 只保存其雜湊。access token MUST NOT 含角色或可見選單（每請求由資料庫取得，FR-021c）。
- **FR-021a**（2026-10-08 新增）: `POST /auth/refresh` MUST 以 refresh Cookie 換發新的 access token，並輪替 refresh token（舊值失效）；session 到期時間 MUST 延長為換發時＋8 小時（滑動）。session 已撤銷、已逾期、找不到，或帳號已刪除、無有效角色時 MUST 回 401 並清除 Cookie（帳號無效時一併撤銷 session）。
- **FR-021b**（2026-10-08 新增）: 出示已輪替的前一個 refresh token 時：距輪替 30 秒內 MUST 視為多分頁並行換新，只換發 access token、不再輪替；超過 30 秒 MUST 視為外洩，撤銷整個 session 並回 401。輪替 MUST 以條件式更新保存（資料庫中仍為原值才寫入）；兩個請求同時以同一個 refresh token 換新時，搶輸的一方 MUST NOT 覆蓋已輪替的值，比照寬限只換發 access token。
- **FR-021c**（2026-10-08 新增）: 每個帶 access token 的請求 MUST 驗證簽章、發行者、對象與效期，並檢查 token 所屬 session 未撤銷、未逾期，且屬於 token 的使用者（`sub`，使用者對外識別碼）；任一不符 MUST 回 401。

**稽核（共通）**

- **FR-020**: 使用者資料的建立與異動 MUST 透過每列稽核欄位（`CreateUser/CreateDate/ModifyUser/ModifyDate`）記錄最後變更者與時間；刪除以軟刪除（`Removed=1`）保留紀錄。逐次變更歷史（完整 audit log）不在本次範圍。

### 邊界情境（Edge Cases）

- 使用者連續多次以錯誤密碼嘗試登入時，於連續 5 次失敗後觸發基本帳號鎖定（約 15 分鐘，見 FR-006a）；鎖定期間即使帳密正確亦一律拒絕，逾時後自動解除。
- 同一來源 IP（如分行共用的對外 IP）1 分鐘內登入請求超過 20 次時，後續請求回 429 直到時間窗結束；其他 IP 不受影響（FR-006b）。
- 使用者在登入狀態下被超級管理員刪除或移除全部角色時，其既有登入工作階段於下次載入頁面時應失去對應選單／受保護頁面存取權。
- 系統中唯一的超級管理員不可被刪除或降級，以避免整個後台失去管理入口（防鎖死）。（⚠️ 已被 SPEC-005 FR-005 取代，不再阻擋。）
- 新增或修改時帳號含前後空白或大小寫差異，應正規化後再比對唯一性，避免視覺上相同卻重複的帳號。
- 指派的角色若已被 002 軟刪除，不可再指派；已指派後角色被刪除時，該角色不再計入使用者的可見選單。
- 密碼強度不足（未達 8 碼或未同時含英文與數字）或為空時，應於欄位層級阻擋並提示（見 FR-013）。（⚠️ 規則已由 SPEC-013 FR-008a 改為 12 碼＋大小寫＋數字。）
- 帳號鎖定計數以正規化帳號為索引、存於行程內（非持久化）：管理員於鎖定期間對該帳號重設密碼或將其軟刪除，皆**不主動清除**鎖定計數，鎖定仍於逾時（約 15 分鐘）後自動解除；被軟刪除之帳號本即無法登入，鎖定與否不影響結果。
- 唯一超級管理員因連續登入失敗遭鎖定時，因鎖定狀態為行程內快取（非資料表欄位，見 Technical Design › 設計決策 R3），**重啟服務即自動解除鎖定**，構成防鎖死之解救途徑；此外初始超管帳號由種子（SeedData）冪等建立，必要時可重新種入以恢復管理入口（見 FR-022）。

### 關鍵實體（Key Entities）

- **使用者（SysUser）**：後台可登入的操作者。屬性含帳號（登入識別碼，未刪除範圍內唯一）、使用者名稱（顯示用）、密碼雜湊（加鹽 SHA-256）、密碼 salt、以及標準稽核欄位與軟刪除旗標（`Removed`）。與角色為多對多關係。密碼明文不落地。
- **角色（SysRoles，2026-09-30 起為 `SysRole`）**：沿用 002 的角色實體；決定使用者可見的選單集合。其中包含受保護、不可刪除的「超級管理員」角色（⚠️ 保護已被 SPEC-005 FR-004 取消）。本功能只讀取／指派角色，不負責角色本身之維護（屬 002）。
- **使用者角色關聯（SysUser ↔ SysRoles，2026-09-30 起表名 `SysUserRole`）**：使用者與角色之間的多對多指派關係，決定該使用者登入後的可見選單（為所屬各角色可見選單之聯集）。

### 假設（Assumptions）

- 規格內容依憲章 Principle VIII 以繁體中文撰寫；章節結構標題與 `FR-###`／`SC-###`／`P1` 等標記保留原樣，作為下游工具解析的結構標記。
- 資料表名稱 `SysUser`、`SysRoles` 與密碼雜湊演算法（加鹽 SHA-256）為使用者明確指定之需求，於本規格中作為既定的資料與安全約束保留；其餘技術實作（框架、儲存、工作階段機制）由技術設計階段決定。（2026-09-30：角色表改為單數 `SysRole`、使用者角色關聯表改為 `SysUserRole`，見 Clarifications Session 2026-09-30。）
- 帳號建立採「由超級管理員於使用者管理頁建立」，不提供公開自助註冊。
- 使用者與角色為多對多關係，與 002「一位使用者可擁有多個角色」一致。
- 選單的顯示與角色↔選單可見性由 002 的機制負責；本功能提供「使用者屬於哪些角色」，可見選單即為這些角色可見選單的聯集（預設拒絕，未授權即不可見）。
- 密碼強度規則：最少 8 碼且須同時包含英文字母與數字（見 FR-013）；本次不含密碼歷史、定期強制更換等進階策略。
- 登入失敗仍回覆一般性錯誤訊息（不區分帳號或密碼何者有誤），並加上基本帳號鎖定——同一帳號連續 5 次失敗鎖定約 15 分鐘、逾時自動解除（見 FR-006a）；更進階的漸進式延遲或 IP 層級節流不在本次範圍。
- 初始資料（至少一個超級管理員帳號）可由 SQL 種入，以確保系統可被首次登入與管理。
- 使用者對角色的指派、以及登入後的選單套用，依賴 002 已建立之角色與可見性資料。
- **明確排除於本次範圍**（採業界合理預設，非本功能需求）：進階無障礙合規（WCAG 2.1 AA 之完整驗證）、多語系（i18n；本功能 UI 僅繁體中文）、瀏覽器相容性矩陣（採現代常青瀏覽器預設）、批量操作（一次新增／刪除多名使用者）、以及同一帳號並發登入數限制（不限制多裝置同時登入）。上述如未來有需求另立功能處理。
- **已定之行為與邊界**（採合理預設並於此載明，避免未來歧義）：
  - **工作階段逾期**（2026-10-08 改寫）：access token 15 分鐘到期後，前端以 refresh token 換新；refresh token 所屬 session 8 小時未換新即逾期，逾期後須重新登入（沿用 FR-001 導回登入頁）。
  - **帳號字元規則**：帳號正規化僅去除「前後」空白並轉小寫；帳號**中間**之空白予以保留（`a b` 與 `ab` 視為不同帳號），且未限制特殊字元（僅約束非全空白且長度 1–50）。
  - **並發寫入**：同一使用者之並發修改（如同時改角色與重設密碼）採「最後寫入者勝出」（last-write-wins），本次不引入樂觀鎖。
  - **登入失敗計數**：以執行緒安全結構累計，避免高併發下之計數競態；計數為行程內狀態（見 Technical Design › 設計決策 R3）。
  - **未預期錯誤**：資料庫不可用等未預期錯誤，回覆一般性伺服器錯誤（HTTP 500，ProblemDetails 格式），不洩漏內部細節。
  - **搜尋規模**：清單查詢每頁上限 100 筆，配合後台規模（數十至數百使用者），不另做超大結果集之特別最佳化。

## Acceptance Criteria（驗收條件）

> 測試檔路徑相對於 `backend/tests/`；同一檔第二次出現只寫類別名。

| AC | 來源 | 條件 | 驗證項 |
|----|------|------|--------|
| AC-1 | US1 情境 1、FR-001 | 未登入存取受保護端點回 401（不回 302），前端守衛導向 `/login` | `TcmClinic.Api.FunctionalTests/AuthEndpointsTests.cs` › `Protected_endpoint_without_login_returns_401`；前端導向：手動：Test Plan 手動走查第 2 步 |
| AC-2 | US1 情境 2、SC-001（正確帳密）、FR-002、FR-021 | 以正確帳密登入回 200，回應含 `accessToken`（`tokenType` 為 `Bearer`）並以 `Set-Cookie` 設定 refresh Cookie；以 Bearer 存取受保護端點成功（2026-10-08 改寫） | `TcmClinic.Api.FunctionalTests/AuthEndpointsTests.cs` › `Login_success_grants_access` |
| AC-3 | US1 情境 3、SC-001（錯誤帳密）、FR-003 | 以錯誤帳密登入的成功率為 0%（無誤放），回 401 一般性訊息 | `AuthEndpointsTests` › `Login_wrong_password_returns_401` |
| AC-4 | US1 情境 4、SC-003、FR-004 | 登入成功者看到的側邊選單，100% 等於其所屬角色被授權可見選單的聯集（無多顯示、無漏顯示） | `TcmClinic.Api.UnitTests/Authorization/GetVisibleMenuRoutesTests.cs` › `Union_excludes_container_and_ungranted`；`AuthEndpointsTests` › `Seeded_admin_sees_menus_and_can_access` |
| AC-5 | US1 情境 5、FR-005 | 登出回 204 並清除 refresh Cookie；之後以同一 access token 存取受保護端點回 401、以原 refresh Cookie 換新回 401；未登入呼叫登出亦回 204（2026-10-08 改寫） | `AuthEndpointsTests` › `Logout_clears_session`、`Logout_without_login_returns_204` |
| AC-6 | US1 情境 6、FR-006、邊界情境 2 | 軟刪除或無有效角色之帳號不能取得後台存取；登入中被刪除者下次請求即 401 | `TcmClinic.Api.FunctionalTests/SessionRevalidationTests.cs` › `Deleted_user_loses_access_next_request`；軟刪除後重新登入被拒：手動：Test Plan 手動走查第 12 步 |
| AC-7 | US2 情境 1、FR-010 | 分頁清單只含未軟刪除使用者，並帶角色代碼／名稱 | `TcmClinic.Api.FunctionalTests/UserCrudEndpointsTests.cs` › `Delete_soft_removes_and_allows_account_reuse`；`TcmClinic.Api.FunctionalTests/Contracts/UsersContractTests.cs` › `Users_list_response_contract` |
| AC-8 | US2 情境 2、FR-010 | 關鍵字（帳號／名稱）搜尋只回符合者 | `UserCrudEndpointsTests` › `Create_user_appears_in_list_and_can_login`（以關鍵字查得）；「只回符合者」：手動：Test Plan 手動走查第 8 步 |
| AC-9 | US2 情境 3、FR-009 | 清單回應不含 `passwordHash`、`passwordSalt` 或明文密碼 | `UserCrudEndpointsTests` › `List_does_not_expose_password`；`UsersContractTests` › `Users_list_response_contract` |
| AC-10 | US3 情境 1、FR-011 | 新增使用者回 201，出現在清單並可以所設帳密登入 | `UserCrudEndpointsTests` › `Create_user_appears_in_list_and_can_login` |
| AC-11 | US3 情境 2、FR-012 | 帳號正規化（trim＋小寫、冪等）後於未刪除範圍唯一；大小寫不同的重複帳號回 400 | `UserCrudEndpointsTests` › `Duplicate_account_case_insensitive_returns_400`；`TcmClinic.Api.IntegrationTests/Data/UserConfigTests.cs` › `Duplicate_account_case_insensitive_is_rejected`；`TcmClinic.Api.UnitTests/UserAggregate/UserAccountTests.cs` › `Normalizes_to_trimmed_lowercase`、`Normalization_is_idempotent` |
| AC-12 | US3 情境 3、FR-013 | 帳號、使用者名稱空白或超長被拒；不合規密碼回 400 | `UserAccountTests` › `Rejects_blank_or_too_long`；`TcmClinic.Api.UnitTests/UserAggregate/UserNameTests.cs` › `Rejects_blank_or_too_long`；`UserCrudEndpointsTests` › `Weak_password_returns_400` |
| AC-13 | US3 情境 4、SC-004、FR-007、FR-008 | 系統中儲存的密碼資料，抽樣檢視 100% 皆為加鹽雜湊、無任何可還原明文；新雜湊為 `pbkdf2-sha512$100000$…` 格式；每次雜湊產生相異 salt，正確密碼驗真、錯誤密碼驗偽；舊 SHA-256 雜湊仍可驗證（2026-10-08 修改） | `TcmClinic.Api.IntegrationTests/Security/Pbkdf2PasswordHasherTests.cs` › `Salt_is_random_per_hash`、`Verify_true_for_correct_password`、`Verify_false_for_wrong_password`、`Hash_uses_pbkdf2_format`、`Verifies_legacy_sha256_hash`、`Needs_rehash_only_for_legacy_or_weaker_hash`；`TcmClinic.Api.UnitTests/UserAggregate/UserTests.cs` › `SetPassword_sets_hash_and_salt` |
| AC-14 | US4 情境 1、FR-014、FR-015 | 改名／換角色回 200 且清單反映；登入中使用者下次請求即反映新角色 | `UserCrudEndpointsTests` › `Update_changes_display_name`；`SessionRevalidationTests` › `Role_change_reflected_next_request` |
| AC-15 | US4 情境 2、FR-014 | 重設密碼後舊密碼失效、僅新密碼可登入 | `UserCrudEndpointsTests` › `Reset_password_invalidates_old` |
| AC-16 | US4 情境 3、FR-016、FR-011 | 角色集合為空時拒絕（網域不變量；新增時回 400） | `UserTests` › `ReplaceRoles_rejects_empty`；`UserCrudEndpointsTests` › `Empty_roles_returns_400` |
| AC-17 | US5 情境 1、FR-017 | 軟刪除回 204、自清單消失；登入中者下次請求即 401 | `UserCrudEndpointsTests` › `Delete_soft_removes_and_allows_account_reuse`；`SessionRevalidationTests` › `Deleted_user_loses_access_next_request` |
| AC-18 | US5 情境 2、SC-007、FR-019 | 原：任何操作皆無法使系統失去唯一的超級管理員存取入口（防鎖死攔截率 100%）。現（SPEC-005 FR-005）：不做防鎖死，刪除唯一超管成功回 204 | `UserCrudEndpointsTests` › `Delete_last_superadmin_succeeds_no_antilockout` |
| AC-19 | US5 情境 3 | 軟刪除後相同帳號可重新建立（過濾唯一索引 `[Removed]=0`） | `UserCrudEndpointsTests` › `Delete_soft_removes_and_allows_account_reuse`；`UserConfigTests` › `Soft_deleted_account_can_be_reused_and_audit_filled` |
| AC-20 | FR-006a、SC-008 | 同一帳號連續 5 次登入失敗後，鎖定期間（約 15 分鐘）的登入嘗試 100% 被拒絕，逾時後可正常登入；成功登入清除計數 | `UserCrudEndpointsTests` › `Locks_account_after_five_failures`；`TcmClinic.Api.IntegrationTests/Security/MemoryLoginThrottleTests.cs` › `Locks_after_five_failures`、`Lockout_expires_after_window`、`Reset_clears_state` |
| AC-21 | FR-013（上限）、FR-014 | 新增與重設密碼超過 128 碼回 400；重設時弱密碼回 400 | `UserCrudEndpointsTests` › `Overlong_password_on_create_returns_400`、`Overlong_password_on_reset_returns_400`、`Weak_password_on_reset_returns_400` |
| AC-22 | FR-022 | 鎖定為行程內狀態，重啟（新實例）不帶前一行程鎖定 | `MemoryLoginThrottleTests` › `Lockout_does_not_persist_across_restart` |
| AC-23 | FR-009（延伸）、Principle XVII | 登入流程日誌不含明文密碼、`PasswordHash`、`PasswordSalt` | `TcmClinic.Api.FunctionalTests/LoggingRedactionTests.cs` › `Login_flow_logs_never_contain_credentials` |
| AC-24 | FR-021 | refresh Cookie 為 HttpOnly、`SameSite=Lax`、`Path=/`，非 Development 環境 `Secure`；access token 效期 15 分鐘、不含角色；開發 CORS 允許憑證（2026-10-08 改寫） | `TcmClinic.Api.FunctionalTests/JwtAuthTests.cs` › `Refresh_cookie_is_http_only_lax`、`Access_token_expires_in_15_minutes_without_roles`；手動：Code Review 確認 `AuthConfigs` 非 Development 時 `Secure` |
| AC-25 | FR-020 | 稽核欄位與 `Removed` 由攔截器填寫 | `UserConfigTests` › `Soft_deleted_account_can_be_reused_and_audit_filled` |
| AC-26 | SC-002 | 使用者於送出登入後 3 秒內看到登入結果（成功進入後台或顯示錯誤） | 手動：Test Plan 手動走查第 3 步 |
| AC-27 | SC-005 | 超級管理員能在 2 分鐘內完成新增一名使用者並指派角色 | 手動：Test Plan 手動走查第 9 步 |
| AC-28 | SC-006 | 使用者清單之關鍵字搜尋於一般負載下 95% 的查詢在 1 秒內回傳結果 | 手動：Test Plan 手動走查第 8 步（以請求日誌延遲確認） |
| AC-29 | Clarifications Session 2026-09-30、R9 | `SysUserRole` 存在、`SysUserRoles` 不存在；外鍵指向 `SysUser`／`SysRole`；唯一索引 `IX_SysUserRole_UserId_RoleId` 篩選 `[Removed]=0` | `TcmClinic.Api.IntegrationTests/Data/MenuRoleSchemaTests.cs` › `System_tables_use_singular_names`、`Join_tables_reference_singular_tables` |
| AC-30 | Clarifications Session 2026-10-02 | 使用者管理端點由 `accountMaintenance` 可見性授權；舊 `/admin/users` 不再出現在選單 | `TcmClinic.Api.FunctionalTests/MenuAuthorizationTests.cs` › `Users_menu_grants_full_user_management`；`AuthEndpointsTests` › `Seeded_admin_sees_menus_and_can_access` |
| AC-31 | FR-007a、Clarifications Session 2026-10-08 | 以舊 SHA-256 雜湊建立的帳號登入成功後，儲存的雜湊改為新格式、密碼修改時間不變，之後仍可用同一密碼登入；新雜湊的帳號登入時不重寫 | `TcmClinic.Api.FunctionalTests/PasswordRehashTests.cs` › `Legacy_hash_is_upgraded_on_login`、`Current_hash_is_not_rewritten_on_login`；`TcmClinic.Api.UnitTests/UserAggregate/UserTests.cs` › `UpgradePasswordHash_keeps_password_changed_at` |
| AC-32 | FR-006b、Clarifications Session 2026-10-08（登入端點限速） | 同一 IP 超過登入次數上限回 429 ProblemDetails 並附 `Retry-After`；不同 IP 分開計算；被拒的請求不計入帳號失敗次數 | `TcmClinic.Api.FunctionalTests/LoginRateLimitTests.cs` › `Exceeding_limit_returns_429_with_retry_after`、`Limit_is_per_client_ip`、`Rejected_requests_do_not_count_toward_lockout` |
| AC-33 | FR-021a | 以 refresh Cookie 換新回 200 與新 access token，並輪替 refresh Cookie；session 到期時間延長為換發時＋8 小時 | `JwtAuthTests` › `Refresh_rotates_tokens_and_slides_expiry` |
| AC-34 | FR-021b | 已輪替的前一個 refresh token 於 30 秒後再出示 → 401，且整個 session 被撤銷（新的 refresh token 與 access token 也失效） | `JwtAuthTests` › `Reused_refresh_token_revokes_session` |
| AC-35 | FR-021b | 已輪替的前一個 refresh token 於 30 秒內再出示 → 200 只換發 access token，不設定新 Cookie | `JwtAuthTests` › `Previous_refresh_token_within_grace_returns_access_token` |
| AC-36 | FR-021c | 過期、簽章不符或對象不符的 access token 回 401 | `JwtAuthTests` › `Invalid_access_token_is_rejected` |
| AC-37 | FR-021a、FR-021c | session 逾期後換新回 401 並清除 Cookie；該 session 的 access token 也回 401 | `JwtAuthTests` › `Expired_session_is_rejected` |
| AC-38 | FR-021a | 帳號被刪除後以 refresh Cookie 換新回 401，session 被撤銷 | `JwtAuthTests` › `Refresh_rejected_for_deleted_user` |
| AC-40 | FR-021c | 帳號刪除後重建同名帳號，舊帳號的 access token（尚未過期）回 401，不得帶著新帳號的權限通過 | `JwtAuthTests` › `Access_token_rejected_after_account_recreated` |
| AC-41 | FR-021b | 兩個請求同時以同一個 refresh token 換新：只有先保存者寫入新值，後者不覆蓋、只換發 access token | `TcmClinic.Api.IntegrationTests/Security/SessionRotationStoreTests.cs`；`TcmClinic.Api.UnitTests/Auth/RefreshSessionHandlerTests.cs` |
| AC-39 | ADR-011 資料表 | `SysUserSession` 欄位、型別、說明、`TokenHash` 與 `PublicId` 唯一索引、外鍵 `UserId`→`SysUser` 符合設計；不寫資料異動紀錄 | `TcmClinic.Api.IntegrationTests/Data/UserSessionSchemaTests.cs` |

> AC-18（US5 情境 2、SC-007）與 FR-018/019/022 自 SPEC-005（2026-08-31）起被取代：不再有超級管理員守門與防鎖死，驗證測試已改為斷言「刪除唯一超管成功」。AC-22 的測試仍存在，但「超管解救途徑」已不屬規範要求。
>
> AC-12、AC-21 的密碼強度自 SPEC-013 FR-008a（2026-09-29）起為 12～128 碼＋大寫＋小寫＋數字，驗證測試已隨之更新。
>
> AC-7 的預設每頁筆數自 SPEC-016 FR-008(b)（2026-10-02）起改取系統參數「查詢列表分頁筆數」；2026-10-06 該參數移除後固定為 10（[ADR-007](../架構決策/ADR-007-default-page-size-fixed.md)）。

## Technical Design（技術設計）

### 技術脈絡

- 語言／框架：C# / .NET 10（`backend/global.json`：SDK 10.0.201、TFM `net10.0`）；前端 Vue 3 + TypeScript（strict）。
- 後端相依（既有，沿用）：FastEndpoints（REPR）、EF Core、Mediator（martinothamar，source-gen）、Ardalis.Result / Ardalis.Specification / Ardalis.GuardClauses、Vogen、Serilog + OpenTelemetry、FluentValidation。
- 後端相依（本功能新增使用，皆框架／BCL 內建、非新增 NuGet 套件）：ASP.NET Core Cookie Authentication（`Microsoft.AspNetCore.Authentication.Cookies`）、`IMemoryCache`、`System.Security.Cryptography`（SHA-256、`RandomNumberGenerator`）。
- 前端相依（既有，沿用）：Pinia、Vue Router、shared `http` client、Vitest + Vue Test Utils。
- 儲存：EF Core → SQL Server（正式／Aspire 容器）或 SQLite（fallback）。新增資料表 `SysUser`、`SysUserRoles`（2026-09-30 改名 `SysUserRole`），皆含標準稽核欄位與 `Removed` 軟刪除（Principle XI）。
- 測試：xUnit v3、NSubstitute、Testcontainers（SQL Server 2022）、`WebApplicationFactory`（`CustomWebApplicationFactory`）；CsCheck（密碼雜湊往返與帳號正規化不變量）、Verify（`/auth`、`/users` 回應 DTO 快照）、NetArchTest（分層規則）；前端 Vitest。
- 目標平台：Linux/Windows 伺服器（API）＋瀏覽器 SPA；專案型態 web（monorepo：`backend/` + `frontend/` + `apphost/`）。
- 效能預算（Principle XIV，沿用預設）：查詢端點 ≤ 300ms p95、寫入端點 ≤ 500ms p95；登入結果於使用者送出後 3 秒內回饋（SC-002）；使用者清單關鍵字搜尋 95% 於 1 秒內（SC-006）。
- 規模：後台管理者規模（數十至數百名使用者）；本功能為模板參考實作。
- 約束：
  - 密碼 MUST 以加鹽 PBKDF2 保存，MUST NOT 落地明文／可還原形式（FR-007~009；2026-10-08 前為 SHA-256）。
  - 帳號唯一性與登入比對 MUST 不分大小寫（正規化為小寫，FR-012）。
  - 密碼 MUST ≥ 8 碼且 ≤ 128 碼、含英文與數字（FR-013；上限防過長輸入之資源濫用）。（⚠️ 現行規則見 SPEC-013 FR-008a。）
  - 連續 5 次登入失敗鎖定約 15 分鐘（FR-006a）——以快取追蹤，不新增 `SysUser` 欄位。
  - Cookie MUST 為 HttpOnly + `SameSite`；開發環境沿用既有 `DevFrontend` CORS（須允許認證憑證）。
  - 角色變更／刪除 MUST 於下次載入頁面即反映（FR-015 + 刪除邊界）——以 `OnValidatePrincipal` 每請求重驗 + 角色刷新達成（R8）。
- 整合方式：以 Cookie 驗證補上正式環境缺少的驗證 scheme；登入成功後簽發 HttpOnly Cookie，`ClaimsPrincipal` 內放入帳號（`Name`）與各角色代碼（`Role` claims），使既有 `GetMyMenu`（依角色代碼聚合可見選單）與授權政策於正式環境生效。
- **日誌與部署（Principle XVII）**：Serilog 以 `ReadFrom.Configuration` 由設定驅動 sink 與最低等級。
  - 開發：`appsettings.json` 只有 Console sink（Aspire dashboard／終端機可看）。
  - 正式／IIS：`appsettings.Production.json` 追加每日輪替 File sink，輸出至站台目錄 `logs/log-YYYYMMDD.txt`，保留最近 31 天（`retainedFileCountLimit`）。IIS 行程無附掛主控台，持久化檔案是產線故障時唯一可靠的診斷紀錄。
  - IIS 部署注意：安裝 .NET Hosting Bundle（含 ASP.NET Core Module）；以 `ASPNETCORE_ENVIRONMENT=Production` 執行才會套用 File sink；**`logs/` 目錄需對 IIS 應用程式集區身分（如 `IIS AppPool\<站台>`）具寫入權限**，否則 File sink 會靜默失敗（屬缺陷），可用 `icacls "C:\inetpub\wwwroot\<站台>\logs" /grant "IIS AppPool\<站台>:(OI)(CI)M"` 授權；調整等級或 sink 只需改 `appsettings.Production.json` 的 `Serilog` 區塊，不需重新編譯。
  - 機密保護（MUST）：密碼明文、雜湊、salt、token、cookie、完整連線字串 MUST NOT 進入日誌；由 `LoggingRedactionTests` 於登入流程把關。

### 受影響元件

| 檔案 | 變更 |
|------|------|
| `backend/src/TcmClinic.Api.Core/UserAggregate/User.cs` | 新增：聚合根（表 `SysUser`），`SetPassword`／`Rename`／`ReplaceRoles`（≥1 角色不變量） |
| `backend/src/TcmClinic.Api.Core/UserAggregate/UserId.cs`、`UserRoleId.cs` | 新增：`[ValueObject<int>]` 強型別 ID |
| `backend/src/TcmClinic.Api.Core/UserAggregate/UserAccount.cs` | 新增：帳號 VO，`Validate`（非空、≤50）＋`NormalizeInput`＝`Trim().ToLowerInvariant()` |
| `backend/src/TcmClinic.Api.Core/UserAggregate/UserName.cs` | 新增：顯示名稱 VO，`Validate`（非空、≤100）＋`NormalizeInput`＝`Trim()` |
| `backend/src/TcmClinic.Api.Core/UserAggregate/UserRole.cs` | 新增：關聯實體（表 `SysUserRole`，原 `SysUserRoles`） |
| `backend/src/TcmClinic.Api.Core/UserAggregate/IPasswordHasher.cs` | 新增：Core 介面（`Hash`／`Verify`） |
| `backend/src/TcmClinic.Api.Core/UserAggregate/Specifications/UserSpecs.cs` | 新增：`UserByAccountSpec`、`UserByIdSpec`、`UsersPageSpec`、`UsersCountSpec` |
| `backend/src/TcmClinic.Api.Infrastructure/Data/Config/UserConfig.cs` | 新增：`SysUser` 映射、Vogen 轉換、過濾唯一索引 |
| `backend/src/TcmClinic.Api.Infrastructure/Data/Config/UserRoleConfig.cs` | 新增：`SysUserRole` 映射（FK、`OnDelete(Restrict)`）；2026-09-30 改 `ToTable("SysUserRole")`（R9） |
| `backend/src/TcmClinic.Api.Infrastructure/Data/ValueGenerators/UserIdValueGenerator.cs`、`UserRoleIdValueGenerator.cs` | 新增：Max+1 ID 產生器 |
| `backend/src/TcmClinic.Api.Infrastructure/Data/Security/Sha256PasswordHasher.cs` | 新增：`IPasswordHasher` 實作（R2） |
| `backend/src/TcmClinic.Api.Infrastructure/Data/Security/MemoryLoginThrottle.cs` | 新增：`ILoginThrottle` 實作（`IMemoryCache`，R3） |
| `backend/src/TcmClinic.Api.Infrastructure/Data/Migrations/20260828041139_AddSysUser.cs` | 新增：建立 `SysUser`、`SysUserRoles` |
| `backend/src/TcmClinic.Api.Infrastructure/Data/Migrations/20261001035329_SingularSysTablesAndMenuParentFk.cs` | 2026-09-30 修訂：與 SPEC-002 共用，`SysUserRoles`→`SysUserRole`（R9） |
| `backend/src/TcmClinic.Api.Infrastructure/Data/AppDbContext.cs` | 調整：新增 `DbSet<User>`／`DbSet<UserRole>` |
| `backend/src/TcmClinic.Api.Infrastructure/Data/SeedData.cs` | 調整：冪等種入初始超管 `admin`（R6） |
| `backend/src/TcmClinic.Api.Infrastructure/InfrastructureServiceExtensions.cs` | 調整：註冊 `IPasswordHasher`／`ILoginThrottle` |
| `backend/src/TcmClinic.Api.UseCases/Auth/Login.cs`、`ILoginThrottle.cs`、`GetUserRolesByAccount.cs` | 新增：登入（含鎖定）、鎖定抽象、每請求重驗用角色查詢 |
| `backend/src/TcmClinic.Api.UseCases/Users/CreateUser.cs`、`GetUsers.cs`、`UpdateUser.cs`、`ResetPassword.cs`、`DeleteUser.cs` | 新增：使用者 CRUD 與重設密碼使用案例 |
| `backend/src/TcmClinic.Api.Web/Endpoints/Auth/LoginEndpoint.cs`、`LogoutEndpoint.cs`、`MeEndpoint.cs` | 新增：`/auth/*` 端點 |
| `backend/src/TcmClinic.Api.Web/Endpoints/Users/CreateUserEndpoint.cs`、`GetUsersEndpoint.cs`、`UpdateUserEndpoint.cs`、`ResetPasswordEndpoint.cs`、`DeleteUserEndpoint.cs` | 新增：`/users*` 端點；2026-10-02 起 `Policies("MenuAccess:accountMaintenance")` |
| `backend/src/TcmClinic.Api.Web/Endpoints/Navigation/GetMyMenuEndpoint.cs` | 調整：改由 `ClaimTypes.Role` claims 取角色代碼（R1） |
| `backend/src/TcmClinic.Api.Web/Configurations/AuthConfigs.cs` | 調整：Cookie 驗證 scheme、`OnRedirectToLogin` 回 401、`OnValidatePrincipal` 每請求重驗（R8） |
| `backend/src/TcmClinic.Api.Web/Configurations/LoggerConfigs.cs`、`backend/src/TcmClinic.Api.Web/Program.cs` | 調整：開發 CORS `AllowCredentials`；Serilog `ReadFrom.Configuration`；種子初始密碼取設定 `Seed:AdminPassword` |
| `backend/src/TcmClinic.Api.Web/appsettings.json`、`appsettings.Production.json` | 調整／新增：Console sink；正式每日輪替 File sink |
| `backend/src/TcmClinic.Api.Web/TcmClinic.Api.Web.csproj`、`Directory.Packages.props`、`THIRD-PARTY-NOTICES.md` | 調整：`Serilog.Sinks.File`、`Serilog.Settings.Configuration`（Principle XVI 登錄） |
| `frontend/src/features/login/*`（`LoginPage.vue`、`auth.store.ts`、`auth.api.ts`、`auth.types.ts`、`routes.ts`、`index.ts`） | 新增：登入功能 |
| `frontend/src/features/user-management/*`（`UserManagementPage.vue`、`user.store.ts`、`user.api.ts`、`user.types.ts`、`routes.ts`、`index.ts`） | 新增：使用者管理功能 |
| `frontend/src/router/index.ts` | 調整：`/login` 路由＋全域守衛 |
| `frontend/src/shared/services/http.ts` | 調整：`credentials:'include'` |
| `frontend/src/App.vue` | 調整：頁首登入者顯示與登出按鈕（FR-005 UI，commit `349703f`） |

> 前端（`frontend/`）為本功能實作當時的範本前端；現行治理範圍僅涵蓋後端，前端由另一團隊維護。

### 資料模型

所有實體遵循 Principle XI（標準欄位）、Principle VII（Vogen ID）。欄位型別以「網域型別（DB 對應）」表示。稽核欄位由 `AuditableEntityBase` 提供、`AuditableEntityInterceptor` 自動填寫，應用程式不得手動設定。

> `SysUser` 之後由 SPEC-013（電話、Email、上次修改密碼時間、`BranchId` 外鍵）與 SPEC-011（機構）擴充欄位，詳見各該規格；下表為本規格範圍內的欄位。

#### 實體：User（表 `SysUser`）

聚合根。`class User : AuditableEntityBase<UserId>, IAggregateRoot`。

| 欄位 | 網域型別 | DB 對應 | 說明 / 規則 |
|------|---------|---------|------------|
| `Id` | `UserId`（Vogen `int`） | PK，`int`，Max+1 `ValueGenerator`（非 IDENTITY） | Principle VII；哨兵 0 於建構時指派 |
| `Account` | `UserAccount`（Vogen `string`） | `Account`，`nvarchar(50)`，NOT NULL | 帳號＝登入識別碼；`NormalizeInput`＝`Trim().ToLowerInvariant()`；唯一（過濾索引，見下） |
| `DisplayName` | `UserName`（Vogen `string`） | `DisplayName`，`nvarchar(100)`，NOT NULL | 使用者名稱（顯示用） |
| `PasswordHash` | `string` | `PasswordHash`，`nvarchar(88)`，NOT NULL | `pbkdf2-sha512$100000$` ＋ Base64(PBKDF2 輸出 32 bytes)；無前綴者為 2026-10-08 前的 Base64(SHA-256(salt⧺password))，登入成功時自動轉換（FR-007a）；封裝，經 `SetPassword`／`UpgradePasswordHash` 設定 |
| `PasswordSalt` | `string` | `PasswordSalt`，`nvarchar(32)`，NOT NULL | Base64(16-byte 隨機 salt) |
| `UserRoles` | `IReadOnlyCollection<UserRole>` | 反向導覽（SysUserRole） | 多對多至 `Role`；至少一個（FR-011/016） |
| `CreateDate/CreateUser/ModifyDate/ModifyUser` | 稽核 | `datetime` / `nvarchar` | `AuditableEntityBase`，攔截器填寫（Principle XI） |
| `Removed` | `bool` | `Removed`，`bit`，NOT NULL，預設 0 | 軟刪除；全域查詢過濾器排除 `Removed=1` |

**索引/約束**：
- `UNIQUE(Account) WHERE [Removed] = 0`（過濾唯一索引）——未刪除範圍內帳號唯一（FR-012）；軟刪除後帳號名稱可重用（US5 情境 3）。

**封裝方法（回傳 `this`，Principle II）**：
- `User(UserAccount account, UserName displayName)`：建構；`Id = UserId.From(0)`（哨兵）。
- `SetPassword(string hash, string salt)`：設定雜湊與 salt（明文永不進入網域）。
- `Rename(UserName displayName)`：改顯示名稱（FR-014）。
- `ReplaceRoles(IEnumerable<RoleId> roleIds)`：重設所屬角色；**MUST 至少一個**，否則擲例外/由 handler 以 `Result.Invalid` 攔截（FR-016）。帳號（`Account`）不提供修改方法（FR-014）。

**不變量**：
- 至少一個角色（FR-011/016）。
- `PasswordHash`/`PasswordSalt` 非空（建立時必經 `SetPassword`）。

#### 實體：UserRole（表 `SysUserRole`，原 `SysUserRoles`；2026-09-30 改單數，R9）

使用者↔角色多對多關聯實體。`class UserRole : AuditableEntityBase<UserRoleId>`。

| 欄位 | 網域型別 | DB 對應 | 說明 |
|------|---------|---------|------|
| `Id` | `UserRoleId`（Vogen `int`） | PK，Max+1 `ValueGenerator` | 代理鍵（沿用 `RoleMenuVisibility` 模式） |
| `UserId` | `UserId`（Vogen） | FK → `SysUser.Id` | `HasForeignKey`；`OnDelete(Restrict)` |
| `RoleId` | `RoleId`（Vogen） | FK → `SysRole.Id`（原 `SysRoles`） | 參照既有 `Role`；`OnDelete(Restrict)` |
| 稽核 + `Removed` | — | 同上 | Principle XI |

**索引/約束**：
- `UNIQUE(UserId, RoleId) WHERE [Removed] = 0`——同一使用者不重複指派同一角色。
- 顯式 `OnDelete(DeleteBehavior.Restrict)`（Principle XI）；使用者刪除採軟刪除，不連動實體刪除。
- 2026-09-30 起主鍵、外鍵、索引名為 EF 預設命名（`PK_SysUserRole`、`IX_SysUserRole_UserId_RoleId` 等）。

#### 參照既有實體：Role（表 `SysRole`，原 `SysRoles`，來自 SPEC-002）

本功能**只讀取/指派**，不新增/修改 `Role` 結構。相關既有成員：
- `Role.Code : RoleCode`（角色代碼）、`Role.Name : RoleName`、常數 `Role.SuperAdminCode = "SUPERADMIN"`、`Role.IsSuperAdmin`。
- 登入時將使用者所屬各 `Role.Code.Value` 作為 `ClaimTypes.Role` 放入 `ClaimsPrincipal`，供授權政策與 `GetMyMenu`（002）消費。

#### 關聯圖

```text
Role (SysRole, 既有) ──< UserRole (SysUserRole) >── User (SysUser)
   1                          FK RoleId / FK UserId        1
   │                                                       │
   └── 其可見選單由 002 的 RoleMenuVisibility 決定           └── 登入後可見選單 = 其所屬各角色可見選單之聯集
```

#### 狀態與生命週期

- **User 狀態**：Active（`Removed=0`）↔ Deleted（`Removed=1`，軟刪除，不可登入、不列於清單）。無「停用」中間態（沿用 002 對角色不設 IsEnabled 的精神）。
- **登入鎖定**：非持久化狀態，存於 `IMemoryCache`（見 R3）；連續 5 次失敗 → 鎖定 15 分鐘 → 逾時自動解除；不屬資料表欄位。
- **工作階段角色刷新**（R8）：登入後使用者的「現時角色」於每次請求由 `OnValidatePrincipal` 依帳號自 DB 重新解析並寫入 `ClaimsPrincipal`；此為執行期行為，**無資料表結構影響**。角色變更／刪除因此於下次載入即生效（FR-015 + 刪除邊界）。

#### 驗證規則彙整（追溯 FR）

| 規則 | 來源 | 落實位置 |
|------|------|---------|
| 帳號未刪除範圍內唯一、不分大小寫 | FR-012 | `UserAccount.NormalizeInput` + 過濾唯一索引 + `UserByAccountSpec` |
| 密碼 ≥ 8 碼且含英文與數字（⚠️ 現行規則見 SPEC-013 FR-008a） | FR-013 | Web FluentValidation + UseCases handler（明文邊界） |
| 密碼加鹽 PBKDF2、不落地明文；登入時自動轉換舊雜湊 | FR-007~009、FR-007a | `IPasswordHasher` / `Pbkdf2PasswordHasher`；`User` 僅存 hash/salt（`UpgradePasswordHash` 不動密碼修改時間）；`LoginHandler` 驗證成功後依 `NeedsRehash` 轉換 |
| 至少一個角色 | FR-011/016 | `User.ReplaceRoles` 不變量 + handler `Result.Invalid` |
| 軟刪除、不可登入、可重用帳號名 | FR-006/017、US5 | `Removed` + 全域過濾器 + 過濾唯一索引 |
| 防鎖死：不可刪最後超管/移除自身超管（⚠️ 已被 SPEC-005 取代） | FR-019 | `DeleteUser`/`UpdateUser` handler 前置檢查 |
| 連續 5 次失敗鎖定 15 分鐘 | FR-006a | `ILoginThrottle` / `MemoryLoginThrottle` |
| 角色變更/刪除下次載入即生效 | FR-015、刪除邊界 | Cookie `OnValidatePrincipal` 每請求重驗 + 角色刷新（R8） |

### API 契約

> 以下兩份為原 API 契約檔，章節標題原樣保留以利程式碼註解追溯。

#### 原 contracts/auth.md — API 契約：驗證（Auth）

Base 路徑沿用既有慣例（前端經 `VITE_API_BASE_URL` 或 `/api` 代理）。錯誤一律 RFC 7807 ProblemDetails。所有 `Summary`/`Description` 於實作時以 zh-TW 撰寫（Principle VIII）。

##### POST /auth/login — 登入

- **Auth**: `AllowAnonymous`
- **對應**: US1、FR-001~004、FR-006/006a/006b

**Request**（`application/json`）:
```json
{ "account": "admin", "password": "P@ssw0rd1" }
```

**Responses**:

| 狀態 | 情境 | Body |
|------|------|------|
| 200 OK | 驗證成功；同時 `Set-Cookie`（HttpOnly refresh Cookie `TcmClinic.Api.Refresh`，2026-10-08） | `LoginResponse`（見下） |
| 400 Bad Request | 請求格式錯誤（欄位空白） | `ValidationProblem` |
| 401 Unauthorized | 帳號不存在、密碼錯誤、帳號已軟刪除/無角色、或**帳號遭鎖定** | ProblemDetails（一般性訊息，**不區分**原因，FR-003） |
| 429 Too Many Requests | 同一來源 IP 1 分鐘內登入請求超過上限（FR-006b，2026-10-08） | ProblemDetails；`Retry-After` 標頭為建議等待秒數（目前為時間窗長度） |

**LoginResponse**:
```json
{ "accessToken": "<JWT>", "tokenType": "Bearer", "expiresIn": 900, "account": "admin", "displayName": "系統管理員", "roles": ["SUPERADMIN"] }
```

**行為要點**:
- 帳號正規化（trim + 小寫）後查找（`Removed=0`）。
- 成功：清除失敗計數；建立 session（`SysUserSession`），回傳 access token 並設定 refresh Cookie（2026-10-08，ADR-011；原為 `SignInAsync` 簽發驗證 Cookie）。
- 失敗：`ILoginThrottle` 計數 +1；達 5 次鎖定 15 分鐘；回 401 一般性訊息（不透露帳號是否存在）。
- 鎖定期間：即使帳密正確亦回 401（FR-006a）。
- 限速：`RequireRateLimiting("login")`，依來源 IP 固定時間窗計數（`RateLimitConfigs`）；超限在進入端點前回 429，不呼叫登入用例（FR-006b）。

##### POST /auth/logout — 登出

- **Auth**: `AllowAnonymous`（2026-10-08 改；原需已登入）
- **對應**: FR-005

| 狀態 | 情境 |
|------|------|
| 204 No Content | 撤銷 refresh Cookie（及有效 access token 的 `sid`）對應的 session，清除 refresh Cookie；冪等，未登入亦回 204 |

##### POST /auth/refresh — 換發 access token（2026-10-08 新增）

- **Auth**: `AllowAnonymous`（以 refresh Cookie 識別）
- **對應**: FR-021a、FR-021b

| 狀態 | 情境 | Body |
|------|------|------|
| 200 OK | 換發成功；輪替時以 `Set-Cookie` 設定新的 refresh Cookie（30 秒寬限內的並行換新不設定） | `LoginResponse`（同登入） |
| 401 Unauthorized | 沒有 refresh Cookie、找不到、已撤銷、已逾期、重用超過寬限，或帳號無效；同時清除 refresh Cookie | ProblemDetails |

##### GET /auth/me — 目前登入者

- **Auth**: 需已登入
- **對應**: US1（登入後前端取得身分與導覽依據）

| 狀態 | 情境 | Body |
|------|------|------|
| 200 OK | 回傳目前 `ClaimsPrincipal` 摘要 | `{ "account": "...", "displayName": "...", "roles": ["..."] }` |
| 401 Unauthorized | 未登入 | ProblemDetails |

> 註：`displayName` 可放入登入時的 claim，或由 `/auth/me` 依帳號回查。登入後「對應選單」由既有 **`GET`（002 Navigation `GetMyMenu`）** 端點提供——該端點須改讀 `ClaimsPrincipal` 的 `Role` claims 作為 `RoleCodes`（見 R1 整合注意）。（現行路徑 `/me/menus`，回應欄位見 SPEC-008。）

##### 未驗證存取受保護端點的行為

> 2026-10-08 起改用 JWT Bearer（ADR-011）：未帶或帶無效 token 時 JwtBearer 回 401（由 ProblemDetails 設定補上內容），不再有 302 轉頁的問題。以下為原 Cookie 設計的紀錄。

Cookie handler 預設對未驗證請求回 302 轉登入頁；本 API 須覆寫 `Events.OnRedirectToLogin` 使 API 路徑改回 **401**（FR-001 於前端由路由守衛導向 `/login`，而非後端 302），避免破壞 CORS/JSON 呼叫。

##### 工作階段每請求重驗與角色刷新（FR-015 + 刪除邊界，R8）

> 2026-10-08 起改在 `JwtBearerEvents.OnTokenValidated` 執行（ADR-011），並加查 session 有效（FR-021c）；行為與下文相同。

Cookie 驗證的 `Events.OnValidatePrincipal` MUST 於**每次已驗證請求**：
- 依 `Name`（帳號）自 DB 查使用者；若不存在／`Removed=1`／無有效角色 → `RejectPrincipal` + 登出（該請求即視為未驗證 → 401，前端守衛導向 `/login`）。
- 否則以 DB 現時角色**重建 `Role` claims**。

因此：
- **角色變更**（US4）於該使用者**下次載入頁面**即反映最新選單（`GetMyMenu` 讀刷新後 claims，FR-015）。
- **被軟刪除／移除全部角色**（US5、邊界）者，其既有工作階段於**下次請求即失去**存取與選單（不需等 Cookie 到期）。
- `GET /auth/me` 與 002 之導覽端點回傳的角色，皆為刷新後之現時角色。

#### 原 contracts/users.md — API 契約：使用者管理（Users）

所有端點 **僅限超級管理員**（`Policies(AuthConfigs.SuperAdminPolicy)`，FR-018）。回傳型別依憲章「Return Type by Operation」；錯誤走 ProblemDetails。**任何回應皆不得含密碼、雜湊或 salt**（FR-009）。`Summary`/`Description` 以 zh-TW 撰寫（Principle VIII）。

> ⚠️ 授權已變更：SPEC-005 起改為選單可見性授權（無權限回 403）；2026-10-02 起 5 支端點皆為 `Policies("MenuAccess:accountMaintenance")`（見 Clarifications Session 2026-10-02）。下文 400「防鎖死」情境亦已被 SPEC-005 FR-005 取消。範例密碼 `P@ssw0rd1` 已不符 SPEC-013 FR-008a 規則（需 12 碼以上且含大小寫與數字）。

DTO 中的角色以 `RoleId`（int）指派、以 `roleCodes`/`roleNames` 呈現，供前端顯示。

##### POST /users — 新增使用者

- **對應**: US3、FR-011~013
- **Return**: `Results<Created<CreateUserResponse>, ValidationProblem, ProblemHttpResult>`

**Request**:
```json
{ "account": "wayne", "displayName": "王先生", "password": "P@ssw0rd1", "roleIds": [1, 2] }
```

| 狀態 | 情境 |
|------|------|
| 201 Created | 建立成功；`Location: /users/{id}`；body＝`{ "id": 5 }` |
| 400 Bad Request | 欄位驗證失敗（帳號空白、密碼未達 8 碼/未含英數、`roleIds` 為空）或**帳號已存在**（`ValidationProblem`） |

**規則**：帳號正規化後於 `Removed=0` 範圍查重（FR-012）；密碼經 `IPasswordHasher` 雜湊後儲存；`roleIds` 至少一個且須為未刪除角色（FR-011/016）。

##### GET /users — 使用者清單（分頁 + 關鍵字）

- **對應**: US2、FR-010
- **Return**: `HandleAsync` + `Send.OkAsync()`；附 GitHub 風格 `Link` 標頭（first/prev/next/last）

**Query 參數**: `?page=1&pageSize=20&keyword=<帳號或使用者名稱>`（`pageSize` 未指定、0 或負數時為 10（[ADR-007](../架構決策/ADR-007-default-page-size-fixed.md)），最大 100，Principle XIV；2026-10-02 前預設 20，2026-10-02～10-06 取系統參數「查詢列表分頁筆數」）

**Response 200**:
```json
{
  "items": [
    { "id": 5, "account": "wayne", "displayName": "王先生", "roleCodes": ["EDITOR"], "roleNames": ["編輯者"] }
  ],
  "page": 1, "pageSize": 20, "totalCount": 1
}
```

**規則**：僅列 `Removed=0`；關鍵字對 `account`/`displayName` 模糊比對；`AsNoTracking` + 投影（兩支 Spec：資料頁 + 計數，Paged Query Convention）；**不含**任何密碼欄位。

##### PUT /users/{id} — 修改使用者（名稱 + 角色）

- **對應**: US4、FR-014~016、FR-019
- **Return**: `Results<Ok<UpdateUserResponse>, NotFound, ProblemHttpResult>`

**Request**:
```json
{ "displayName": "王大明", "roleIds": [2, 3] }
```
（帳號 `account` **不可修改**，FR-014；故不在請求體。）

| 狀態 | 情境 |
|------|------|
| 200 OK | 更新成功；body＝更新後摘要（含 `roleCodes`） |
| 404 Not Found | 找不到（或已軟刪除）該使用者 |
| 400 Bad Request | `roleIds` 為空（FR-016）；或**移除當前登入者自身之超級管理員角色 / 移除系統最後一位超管**（FR-019 防鎖死） |

##### PUT /users/{id}/password — 重設密碼

- **對應**: US4、FR-014、FR-013
- **Return**: `Results<NoContent, NotFound, ProblemHttpResult>`

**Request**:
```json
{ "newPassword": "N3wP@ssw0rd" }
```

| 狀態 | 情境 |
|------|------|
| 204 No Content | 以新密碼之新 salt+雜湊覆寫；舊密碼即失效 |
| 404 Not Found | 找不到該使用者 |
| 400 Bad Request | 新密碼未達強度規則（FR-013） |

##### DELETE /users/{id} — 刪除使用者（軟刪除）

- **對應**: US5、FR-017、FR-019
- **Return**: `Results<NoContent, NotFound, ProblemHttpResult>`

| 狀態 | 情境 |
|------|------|
| 204 No Content | 軟刪除（`Removed=1`）；該帳號自清單消失且無法登入 |
| 404 Not Found | 找不到該使用者 |
| 400 Bad Request | **刪除系統最後一位超級管理員 / 刪除自己**（FR-019 防鎖死） |

##### 契約快照（Principle XV）

以 Verify 對上述端點之 OpenAPI 文件與回應 DTO 形狀建立快照；任何欄位名稱/型別/狀態碼變更須使快照失敗並於 PR 內重新核可。

### 設計決策（原 research）

> 決策編號 R1～R9 沿用原研究文件，供程式碼註解（如 `research R1`、`research R9`）追溯。

#### R1 驗證與工作階段機制（Cookie vs JWT vs Session）（⚠️ 2026-10-08 由 [ADR-011](ADR-011-jwt-bearer-authentication.md) 取代，改用 JWT Bearer）
- **決定**：採 **ASP.NET Core Cookie Authentication**（`AddAuthentication().AddCookie(...)`）。登入成功後以 `HttpContext.SignInAsync` 簽發 HttpOnly Cookie，`ClaimsPrincipal` 內含 `ClaimTypes.Name = 帳號（正規化小寫）` 與每個所屬角色的 `ClaimTypes.Role = 角色代碼(RoleCode)`。登出以 `SignOutAsync` 清除 Cookie。
- **理由**：
  - 正式環境原本**無任何驗證 scheme**（`builder.Services.AddAuthentication()` 為 no-op），導致 002/003 受 `SuperAdminPolicy`（`RequireRole(Role.SuperAdminCode)`）保護之端點在正式環境會 500。本功能為登入入口，正好補上此 scheme，一次讓既有授權政策生效。
  - 既有 `HttpContextCurrentUser.UserName` 取 `User.Identity?.Name`；既有 `SuperAdminPolicy` 用 `RequireRole`；既有 `GetMyMenu` 依「角色代碼清單」聚合可見選單。Cookie 帶入 `Name` + `Role` claims 後，這三者**無需改寫即生效**——整合面最小。
  - HttpOnly Cookie 由瀏覽器自動附帶，前端**不需在 JS 儲存 token**（避免 XSS 竊取），`shared/http.ts` 僅需 `credentials:'include'`；符合「同套後端+前端」情境。
- **替代方案**：
  - **JWT（Bearer）**：需前端保存 token（localStorage 有 XSS 風險；記憶體則刷新即失效）、手動注入 `Authorization` 標頭、另做刷新機制。對本單體後台過度複雜，且與既有 `RequireRole`/`Identity.Name` 整合較迂迴。否決。
  - **伺服器端 Session（`ISession` + 分散式快取）**：需額外 session store 設定；Cookie 驗證已能承載身分與角色 claims，Session 屬多餘狀態。否決（YAGNI）。
- **整合注意**：
  - `GetMyMenuEndpoint`（002）須以 `User.FindAll(ClaimTypes.Role).Select(c => c.Value)` 取得角色代碼傳入 `GetMyMenuQuery`。若既有端點改由請求參數/測試提供角色，須將其改讀 claims（列為整合任務）。**角色 claims 於每次請求由 R8 的重驗機制自 DB 刷新**，故讀 claims 即為現時角色。
  - 開發環境 CORS：既有 `DevFrontend` 用 `AllowAnyOrigin()`，**與 `AllowCredentials()` 互斥**。接 Cookie 後須改為 `WithOrigins(<vite origin>).AllowCredentials()`（否則瀏覽器不送 Cookie）。
  - Cookie 屬性：`HttpOnly=true`、`SameSite=Lax`（同站導覽即可帶；若前後端不同來源需 `SameSite=None; Secure`）、正式環境 `SecurePolicy=Always`。
  - 未驗證存取受保護端點時，Cookie handler 預設 302 轉登入頁；對 API 應改回 **401**（`OnRedirectToLogin` 覆寫為回 401），讓前端守衛處理導向，避免 CORS/JSON 破壞。

#### R2 密碼雜湊：加鹽 SHA-256（遵循明示需求）

> ⚠️ **已被取代（2026-10-08）**：改為 PBKDF2，並於登入成功時自動轉換既有雜湊，見 [ADR-009](ADR-009-password-hashing-pbkdf2.md)。以下保留為歷史紀錄。
- **決定**：於 Core 定義 `IPasswordHasher`；Infrastructure 以 `Sha256PasswordHasher` 實作：
  - 每次建立／重設密碼時以 `RandomNumberGenerator` 產生 **16 bytes 隨機 salt**。
  - `hash = SHA256(saltBytes ⧺ UTF8(password))`；`PasswordHash`、`PasswordSalt` 皆以 **Base64** 字串儲存。
  - 驗證：以儲存 salt 重算雜湊，並以**固定時間比較**（`CryptographicOperations.FixedTimeEquals`）比對，避免時序側通道。
- **理由**：使用者明確指定「密碼用 SHA256 去加密，還有加 salt」，且欄位僅限帳號/密碼/salt/使用者名稱/角色。本決策忠實遵循該需求，並補上業界必要細節（CSPRNG salt、固定時間比較）而不擴張欄位。以 `IPasswordHasher` 抽象封裝演算法：日後若要升級為慢雜湊（PBKDF2/bcrypt/Argon2）或多輪迭代，只需替換單一 Infrastructure 實作，不動 Core/UseCases/Web（Principle I）。
- **替代方案**：
  - **PBKDF2 / bcrypt / Argon2（慢雜湊 KDF）**：對離線暴力破解更耐受，是純密碼儲存的最佳實務；但偏離使用者明示的 SHA-256 需求，故不作為本次實作，僅記錄為未來強化路徑。
  - **全域固定 pepper**：可加，但需安全保存密鑰且非明示需求，列為未來選項，不在本次範圍。
- **安全備註（誠實揭露）**：SHA-256 為快速雜湊，即使加 salt，對強力離線攻擊之保護弱於慢雜湊。此為**明示需求下的已知取捨**（見「原則符合性」與「Risks」）。

#### R3 登入失敗鎖定（FR-006a）：IMemoryCache 追蹤
- **決定**：定義 `ILoginThrottle`（UseCases/Auth）；Infrastructure 以 `MemoryLoginThrottle`（`IMemoryCache`）實作：
  - Key = 正規化帳號。記錄「連續失敗次數」與「鎖定截止時間」。
  - 連續失敗達 **5 次** → 設定鎖定至 **now + 15 分鐘**；鎖定期間任何登入嘗試（含正確帳密）一律拒絕。
  - 登入成功 → 清除該帳號計數。逾時（TTL）→ 計數自動失效、鎖定自動解除。
- **理由**：規格釐清決議「不新增 `SysUser` 欄位」；`IMemoryCache` 內建、零新增相依、實作簡單，符合模板規模。鎖定與帳號是否存在無關（對不存在帳號也計數），避免以「是否被鎖定」反推帳號存在性（配合 FR-003 一般性錯誤訊息）。
- **替代方案**：
  - **DB 欄位（`FailedCount`/`LockoutEnd`）於 SysUser**：可跨重啟/多實例持久，但擴張使用者明示的欄位集合。否決。
  - **漸進式延遲 / IP 節流**：更強但屬後續增強（假設已列範圍外）。否決（本次）。
- **維運警示（記錄於實作註解）**：`IMemoryCache` 為**行程內**狀態——服務重啟會清空鎖定計數，多實例部署下各節點計數獨立。若未來需跨實例一致的鎖定，改用分散式快取（如 Redis）替換 `ILoginThrottle` 實作即可，介面不變。

#### R4 `SysUser` / `SysUserRoles` 資料模型與唯一性
- **決定**：見「資料模型」。要點：
  - `User : AuditableEntityBase<UserId>, IAggregateRoot`，含 `Account`(UserAccount)、`DisplayName`(UserName)、`PasswordHash`、`PasswordSalt`，與 `UserRole` 集合。
  - `UserRole : AuditableEntityBase<UserRoleId>`，FK `UserId` + `RoleId`（Vogen 型別）。
  - 帳號唯一性以 **過濾唯一索引** `UNIQUE(Account) WHERE [Removed]=0` 實作（沿用 002 之 filtered unique index 慣例），使軟刪除帳號名稱可重用（US5 情境 3）。
  - `UserAccount` 之 `NormalizeInput` 進行 `Trim().ToLowerInvariant()`，令唯一性與登入比對天然不分大小寫（FR-012）。
- **理由**：完全對齊既有 `Role`/`RoleMenuVisibility` 的建模與 Principle XI；唯一性在正規化 + 過濾索引雙重保證下，避免大小寫/空白造成的視覺重複帳號。
- **替代方案**：
  - 以資料庫 `COLLATE` 達成不分大小寫：依賴特定 DB 定序、SQLite/SQL Server 行為不一，改以應用層正規化更可攜。否決。
  - 複合主鍵 `(UserId, RoleId)` 作為 `SysUserRoles`：可行，但既有 `RoleMenuVisibility` 採單一 Vogen 代理鍵 + `ValueGenerator`，為一致性採相同模式。否決。

#### R5 密碼強度驗證位置（FR-013）
- **決定**：密碼明文之強度規則（≥ 8 碼且含英文與數字）在 **Web 請求 FluentValidation** 與 **UseCases 建立/重設 handler** 兩處驗證（Principle IV 防禦縱深）；`User` 網域只接受「已雜湊」的密碼（`SetPassword(hash, salt)`），不接觸明文。
- **理由**：明文只存在於「請求 DTO → handler 呼叫 `IPasswordHasher`」的最短路徑；網域與持久層永不見明文（FR-007/009）。正規表達式類規則放在邊界，避免污染網域模型。
- **替代方案**：將密碼規則做成 Vogen `Password` VO——但 VO 會短暫承載明文且易被記錄，違反「明文不落地/不外洩」精神。改以邊界驗證 + 立即雜湊。否決。

#### R6 初始超級管理員種子（可首次登入）
- **決定**：於既有 `SeedData.SeedAsync` 內，於既有 SUPERADMIN 角色種子之後，冪等種入一個初始超級管理員 `User`（帳號如 `admin`，密碼經 `IPasswordHasher` 雜湊），並建立其 `UserRole` 指向 SUPERADMIN。以 `IgnoreQueryFilters` 檢查是否已存在避免重複。初始密碼由設定（`IConfiguration`）提供，開發預設值僅供本機使用並於文件標註「首次登入後應更換」。
- **理由**：無初始帳號則無人能登入管理（SC-001/防鎖死）。種子須用 `IPasswordHasher` 走正式雜湊路徑，確保與登入驗證一致。
- **替代方案**：純 SQL `HasData` 種入固定雜湊——但雜湊需與執行期 hasher 一致且 salt 隨機，以程式化種子較穩妥。否決。

#### R7 測試策略（對齊 Principle V/XII/XV）
- **決定**：
  - **Unit（先寫）**：`UserAccount`/`UserName` VO 驗證與正規化；`User` 行為（建立、改名、換角色、`SetPassword`）；`Sha256PasswordHasher` 之 **property-based**（CsCheck）不變量——同一密碼+salt 雜湊一致、正確密碼驗證恆真、任意不同密碼驗證恆偽、`NormalizeInput` 冪等（正規化兩次＝一次）。
  - **Integration**：`UserConfig` 映射與過濾唯一索引（重複帳號被拒、軟刪除後可重用）；`MemoryLoginThrottle` 之 5 次鎖定/逾時解除。
  - **Functional（對真實 SQL，`CustomWebApplicationFactory`）**：登入成功/失敗/鎖定、登出、未授權受保護端點回 401；使用者 CRUD 全流程；重設密碼後舊密碼失效；防鎖死（不可刪最後超管）。
  - **Contract（Verify 快照）**：`/auth/*`、`/users/*` 之 OpenAPI 與回應 DTO 形狀。
  - **Architecture（NetArchTest）**：確認 `UserAggregate`/`Users`/`Auth` 命名空間未反向相依。
- **理由**：直接沿用既有測試基礎設施（Testcontainers、`TestAuthHandler`）。登入/鎖定/密碼屬高風險邏輯，以 property-based + functional 雙重把關；契約快照防止 API 形狀漂移（Principle XV）。
- **替代方案**：僅以範例式單元測試涵蓋密碼——不足以涵蓋邊界，違反 Principle XV 對核心邏輯的 property-based 要求。否決。

#### R8 工作階段即時性：撤權／刪除於下次載入即失效（FR-015 + 刪除邊界）（2026-10-08 起改在 JWT 驗證後執行，見 ADR-011）
- **背景（一致性分析發現 I1）**：若僅把角色代碼「烘焙」進登入時的 Cookie claims，則使用者在登入狀態下被改角色或軟刪除時，其 Cookie 仍帶舊 claims，要到**重新登入**才反映——這與「**下次載入頁面**即套用最新角色」（FR-015）及邊界條件「登入中被刪除／移除全部角色 → 下次載入即失去存取」相衝突，且被刪除者在 Cookie 到期前仍可通過授權（安全風險）。
- **決定**：以 **Cookie 驗證的 `Events.OnValidatePrincipal`** 於**每次已驗證請求**重新驗證並刷新：
  1. 依 `ClaimTypes.Name`（帳號）自 DB 查該使用者（**含** `Removed` 判定）。
  2. 若使用者不存在或 `Removed=1` 或**無任何有效角色** → `context.RejectPrincipal()` + `SignOutAsync`，使該請求視為未驗證（→ 401 / 前端守衛導向登入頁）。
  3. 否則以 DB 現時角色**重建 `ClaimTypes.Role` claims**，令 `GetMyMenu` 與授權政策皆依現時角色運作（FR-015、FR-004、FR-018）。
- **理由**：
  - 直接滿足「下次載入頁面即反映」語意：每次頁面載入/ API 請求都會過 `OnValidatePrincipal`。
  - 撤權與刪除**即時生效**，消除「Cookie 到期前仍有存取」的安全窗（呼應 FR-006/017/019 精神）。
  - 角色改由每請求 DB 刷新後，`GetMyMenu` 讀 claims 即為現時值——維持 R1 的整合方式不變。
- **成本與緩解**：每請求多一次輕量使用者查詢。對後台管理者規模（數十～數百人）可接受；查詢用 `AsNoTracking` + 依帳號之過濾唯一索引，成本極低（Principle XIV）。若未來流量放大，可加入 `ValidationInterval`（例如每 N 分鐘才重驗一次，介於即時性與成本之間），並將重驗結果短暫快取；本次採「每請求重驗」以最貼近規格的即時語意。
- **替代方案**：
  - **短效 Cookie + 純重新登入**：無法滿足「下次載入即反映」，且撤權延遲到期限。否決。
  - **每請求都重新查角色但不檢查存在性**：無法讓「被刪除者」即時失去存取。否決（須含 `Removed` 檢查）。
  - **改用伺服器端 Session 保存角色**：等同引入額外狀態儲存；`OnValidatePrincipal` 已足夠。否決（YAGNI）。
- **對任務的影響**：新增實作任務（`OnValidatePrincipal` 重驗與角色刷新，T033/T034）與對應功能測試（登入中被移除角色/軟刪除 → 下次請求即 401 且選單即時更新，T029）。

#### R9 使用者角色關聯表改單數（2026-09-30；DATABASE-SCHEMA §8；Clarifications Session 2026-09-30）
- **決定**：`SysUserRoles` 以 `RenameTable` 改為 `SysUserRole`，保留資料；主鍵、外鍵、索引名隨之改為 EF 預設命名（`PK_SysUserRole`、`IX_SysUserRole_UserId_RoleId` 等）。角色表由 002 同步改為 `SysRole`（SPEC-002 Technical Design › 設計決策 R8），兩者於同一個 migration 完成，避免外鍵名稱跨兩次變更。
- **理由**：資料表名一律單數（§8；使用者 2026-09-30 裁決，取代 2026-08 沿用指定表名的做法）。C# 類別名（`UserRole`）不變。
- **替代方案**：只改 002 的三張表、本表維持複數——命名不一致，且外鍵名稱仍會隨 `SysRole` 改名而變動，分開做沒有好處。

> 未解決項目：無。技術脈絡的未定項已於上述解決；規格無殘留待釐清標記。一致性分析發現之 I1（FR-015 工作階段即時性）已由 R8 解決。

### 原則符合性

對照 [docs/CONSTITUTION.md](../../../docs/CONSTITUTION.md)：初次與設計後複查皆 PASS，無未說明的違規。要點：Clean Architecture（`User`/VO/`IPasswordHasher` 於 Core，實作於 Infrastructure/Web，相依向內）；DDD 聚合封裝；CQRS（`Login`/`CreateUser`/`UpdateUser`/`ResetPassword`/`DeleteUser` 為 Command、`GetUsers` 為 Query，Login 有鎖定副作用故為 Command）；分層驗證（FluentValidation＋使用案例＋GuardClauses＋網域不變量）；測試先行；Vogen 強型別 ID（Max+1）；資料表慣例（標準欄位、Vogen FK、過濾唯一索引、顯式 `OnDelete`）；UX 一致性（ProblemDetails、清單分頁＋`Link` 標頭）；效能（`AsNoTracking`、投影、分頁、無 N+1）；測試護欄（property-based、Verify 契約快照、NetArchTest）；相依治理（Cookie 驗證／`IMemoryCache`／SHA-256 皆框架或 BCL 內建；後補的 `Serilog.Sinks.File`、`Serilog.Settings.Configuration` 已登錄 `THIRD-PARTY-NOTICES.md`）。

明示取捨（Complexity Tracking）：

| 取捨 | 為何需要 | 較簡單方案被否原因 |
|------|---------|-------------------|
| ~~密碼採 SHA-256+salt（非慢雜湊 KDF）~~ | ~~使用者明確指定的既定需求~~ | 2026-10-08 已改為 PBKDF2，此項不再適用（[ADR-009](ADR-009-password-hashing-pbkdf2.md)） |
| 登入鎖定以 `IMemoryCache` 追蹤 | 釐清決議「不新增 `SysUser` 欄位」；模板規模單機足夠 | 加 DB 欄位（`FailedCount`/`LockoutEnd`）會擴張使用者明示的欄位集合；分散式快取為 YAGNI（多實例警示記於 R3） |
| 新增 Cookie 驗證 scheme | 正式環境原無 scheme，`RequireRole` 無法生效（既有 500 caveat）；本功能為登入入口 | JWT 需前端存 token（localStorage → XSS 風險）且手動注入標頭；同套後端+前端下 HttpOnly Cookie 更安全簡單 |

**日誌合規（Principle XVII，於本功能實作後才加入憲章，2026-08-28 補）**：
- 功能層 PASS：機密不得入日誌——`LoggingBehavior` 僅記 `typeof(TMessage).Name`（不序列化含密碼的 `LoginCommand`/`ResetPasswordCommand`），`UseSerilogRequestLogging` 只記方法/路徑/狀態/延遲（不記 request body），密碼明文僅存於「端點 → handler 呼叫 `IPasswordHasher`」之最短路徑（FR-007/009）。一致性分析 X1（缺測試護欄）已由 `LoggingRedactionTests` 補上（T088）。
- 持久化 sink（一致性分析 X3）：原全站僅 `WriteTo.Console()`；因當時無其他進行中功能，收斂於本功能 Phase 9（T089–T091）完成（見「技術脈絡 › 日誌與部署」）。此屬全站設定，若日後另立「Production 日誌持久化」功能可移轉。

## Test Plan（測試計畫）

### 自動化測試

- [x] 單元測試 `backend/tests/TcmClinic.Api.UnitTests/UserAggregate/UserAccountTests.cs`、`UserNameTests.cs`、`UserTests.cs`：VO 驗證與正規化、`User` 行為（AC-11、AC-12、AC-13、AC-16）。
- [x] 單元測試 `backend/tests/TcmClinic.Api.UnitTests/Authorization/GetVisibleMenuRoutesTests.cs`：多角色可見選單聯集（AC-4）。
- [x] 架構測試 `backend/tests/TcmClinic.Api.UnitTests/Architecture/ArchitectureTests.cs`：Core／UseCases（含 `UseCases.Auth`）不得反向相依。
- [x] 整合測試 `backend/tests/TcmClinic.Api.IntegrationTests/Security/Sha256PasswordHasherTests.cs`：雜湊往返、salt 隨機（AC-13）。
- [x] 整合測試 `backend/tests/TcmClinic.Api.IntegrationTests/Security/MemoryLoginThrottleTests.cs`：鎖定、逾時、清除、重啟不保留（AC-20、AC-22）。
- [x] 整合測試 `backend/tests/TcmClinic.Api.IntegrationTests/Data/UserConfigTests.cs`：過濾唯一索引、軟刪除重用、稽核欄位（AC-11、AC-19、AC-25）。
- [x] 整合測試 `backend/tests/TcmClinic.Api.IntegrationTests/Data/MenuRoleSchemaTests.cs`：`SysUserRole` 單數表名與外鍵（AC-29）。
- [x] 功能測試 `backend/tests/TcmClinic.Api.FunctionalTests/AuthEndpointsTests.cs`：真實 Cookie 登入／登出／401（AC-1～AC-5、AC-30）。
- [x] 功能測試 `backend/tests/TcmClinic.Api.FunctionalTests/SessionRevalidationTests.cs`：R8 每請求重驗（AC-6、AC-14、AC-17）。
- [x] 功能測試 `backend/tests/TcmClinic.Api.FunctionalTests/UserCrudEndpointsTests.cs`：CRUD、密碼規則、鎖定、清單不外洩（AC-7～AC-21）。
- [x] 功能測試 `backend/tests/TcmClinic.Api.FunctionalTests/LoggingRedactionTests.cs`：日誌不含機密（AC-23）。
- [x] 功能測試 `backend/tests/TcmClinic.Api.FunctionalTests/MenuAuthorizationTests.cs`：`accountMaintenance` 授權（AC-30）。
- [x] 契約快照 `backend/tests/TcmClinic.Api.FunctionalTests/Contracts/AuthContractTests.cs`、`UsersContractTests.cs`（AC-2、AC-7、AC-9）。
- [x] 前端 Vitest `frontend/src/features/login/auth.store.spec.ts`、`frontend/src/features/user-management/user.store.spec.ts`（登入狀態、清單／新增／修改／重設／刪除 store 行為）。

執行（需 Docker Desktop）：

```powershell
dotnet run --project backend/tests/TcmClinic.Api.FunctionalTests -- -class "TcmClinic.Api.FunctionalTests.UserCrudEndpointsTests"
dotnet run --project backend/tests/TcmClinic.Api.IntegrationTests -- -class "TcmClinic.Api.IntegrationTests.Security.MemoryLoginThrottleTests"
dotnet run --project backend/tests/TcmClinic.Api.UnitTests
```

預期：全綠。

### 手動走查

1. 啟動：`dotnet run --project apphost/TcmClinic.Api.AspireHost`（或後端單獨 `dotnet run --project backend/src/TcmClinic.Api.Web`）。開發環境啟動時自動套用 migration 並執行種子（SUPERADMIN 角色與初始超管 `admin`，初始密碼取設定 `Seed:AdminPassword`，未設定時用開發預設值）。API 文件：`/scalar`、`/swagger`。
2. 瀏覽器開啟前端根路徑 → 應被導向 `/login`（未登入，FR-001）。
3. 以初始超管帳密登入 → 3 秒內進入後台首頁（SC-002）；側邊選單顯示其角色可見選單（FR-004，經 `/me/menus`，讀 Cookie 的 `Role` claims）。
4. 以錯誤密碼登入 → 停留登入頁、顯示一般性錯誤（不透露帳號或密碼，FR-003）。
5. 連續 5 次錯誤 → 第 6 次即使正確亦被拒（鎖定約 15 分鐘，FR-006a）。
6. 登出 → 再次存取受保護頁面被導回 `/login`（FR-005）。
7. API 快速驗證：`curl -i -c cookies.txt -X POST http://localhost:5000/auth/login -H "Content-Type: application/json" -d '{"account":"admin","password":"<初始密碼>"}'`；`curl -s -b cookies.txt http://localhost:5000/auth/me`；`curl -i -b cookies.txt -X POST http://localhost:5000/auth/logout`。
8. 登入後 `GET /users?page=1&pageSize=20&keyword=wa` → 回分頁清單＋`Link` 標頭，只含帳號或名稱含 `wa` 者；回應不含任何密碼／雜湊／salt 欄位（FR-009/010）；於請求日誌確認延遲在 1 秒內（SC-006）。
9. `POST /users`（`{"account":"Wayne","displayName":"王先生","password":"P@ssw0rd1234","roleIds":[1]}`）→ 201＋`Location`；以 `WAYNE` 重複新增 → 400「帳號已存在」（FR-012）；不合規密碼 → 400（FR-013，現行規則見 SPEC-013）；`roleIds: []` → 400（FR-011/016）；新帳號可用所設帳密登入並看到其角色選單。以前端畫面完成一次新增＋指派角色，計時 2 分鐘內（SC-005）。
10. `PUT /users/{id}`（改名／換角色）→ 200；`roleIds: []` → 400（FR-016）。`PUT /users/{id}/password` 設新密碼 → 204；該使用者僅能用新密碼登入、舊密碼失效（FR-014）。
11. 工作階段即時性（FR-015＋刪除邊界，R8）：使用者 X 登入中；改變 X 的角色 → X 下次載入頁面時側邊選單即反映最新角色；軟刪除 X 或使其無有效角色 → X 的下次請求即 401 → 導向 `/login`。
12. `DELETE /users/{id}` → 204；該帳號自清單消失，以其帳密登入回 401（FR-006/017）；以相同帳號名稱重新 `POST /users` → 201（US5 情境 3）。
13. 防鎖死：原規格為「刪除最後一位超管／刪除自己／移除自身超管角色 → 400」；自 SPEC-005 起不再攔截（刪除唯一超管回 204）。

## Dependencies（相依）

- SPEC-002 後台選單與角色權限管理：提供 `SysRole`（原 `SysRoles`）、「超級管理員」角色，以及角色↔選單可見性規則。本功能的「登入後取得對應選單」建立於此之上；`SysRole` 改名與本功能 `SysUserRole` 改名共用同一 migration。
- SPEC-005 選單導向授權模型：取代本規格 FR-018/019/022，使用者管理端點改依選單可見性授權。
- SPEC-007 CHB選單與角色種子：`accountMaintenance`（帳號維護）選單節點。
- SPEC-008 選單API欄位對齊前端：`/me/menus` 回應契約。
- SPEC-013 個人資料維護：修改 FR-013 密碼規則、擴充 `SysUser` 欄位。
- SPEC-016 系統參數維護：使用者清單預設每頁筆數。

## Risks（風險）

- ~~密碼採快速雜湊 SHA-256（即使加 salt），對強力離線暴力破解的保護弱於慢雜湊 KDF。~~ 2026-10-08 已改為 PBKDF2 並於登入時自動轉換舊雜湊（[ADR-009](ADR-009-password-hashing-pbkdf2.md)）；長期未登入的帳號在下次登入或重設密碼前仍為舊雜湊。
- 登入鎖定為行程內狀態：服務重啟即清空、多實例部署下各節點計數獨立（R3）。
- 每請求重驗多一次 DB 查詢；目前規模可接受，流量放大時需考慮 `ValidationInterval` 與短暫快取（R8）。
- 登入限速依 `RemoteIpAddress` 分區（FR-006b）：部署架構若在 IIS 前另有負載平衡或反向代理，所有請求的來源 IP 都會是代理位址，全行共用同一份額度（每分鐘 20 次），等於登入癱瘓。部署前必須確認架構；有代理時須設定 `ForwardedHeaders` 並以 `KnownProxies`／`KnownNetworks` 限定可信任的代理，否則 `X-Forwarded-For` 可被偽造而繞過限速（`docs/INFRASTRUCTURE.md`）。
- 登入限速的已知限制：IPv6 以完整位址分區，持有整段位址者可輪換位址繞過；被擋的每個請求各記一筆 `Warning`，遭大量請求時日誌量會增加。行內網路環境下影響有限。
- 自 SPEC-005 起已無防鎖死，刪除唯一管理帳號或清空權限後須人工修改資料表恢復。
- SC-002、SC-005、SC-006 無自動化計時測試，僅能手動驗證。
- 規格審查（全面檢查清單）留下的非阻斷缺口：「一般性錯誤訊息」具體文案、「約 15 分鐘」容差、多角色清單顯示格式、SC-004 抽樣數、SC-006「一般負載」量化、重設密碼不驗舊密碼的理由、002 不可用時的降級行為、SQL 注入防護僅依 ORM 隱含、關鍵字搜尋是否支援萬用字元等未明文定義。

## 實作紀錄（Archive）

- **狀態**：已實作；2026-09-30 資料表規範對齊修訂與 2026-10-02 舊後台選單移除皆已完成。
- **主要提交**：`d1eba87` 初版（SysUser＋Cookie 驗證）→ `4946c49` 日誌持久化與機密遮罩（Phase 9）→ `4e1d8bf` 檢查清單全面審查與規格補強（FR-013 上限／FR-021／FR-022）→ `c2eb95a` 密碼上限與超管解救測試（Phase 10）→ `349703f` 前端登出 UI → `d605d89` 全測試補 zh-TW DisplayName（Phase 11）→ `33add5c` .NET 版本更正 → `d387981` 被 005 取代之 FR 加前向註記 → `3f2ed74`／`d42bdef`／`e5d3f1b`／`63c9b7f`／`97a56cc` 資料表單數化收斂（T096–T098）→ `fa91926` 預設每頁筆數改取系統參數（SPEC-016）→ `c8e5872` 移除舊後台選單。
- **審查紀錄**：規格品質檢查（requirements）全數通過；全面需求審查（comprehensive，120 項）於 2026-08-29 完成，101 項滿足、19 項部分滿足／建議澄清（CHK007、015、020、021、022、024、043、044、047、051、052、065、072、073、075、098、108、110、112），並據此補入 FR-021、FR-022 與 Assumptions 排除清單／已定行為。
- **測試數**：初版 UnitTests 29、IntegrationTests 10、FunctionalTests 18、前端 Vitest 10；Phase 9 後 FunctionalTests 33；Phase 11 時全測試 75 個皆帶 DisplayName；2026-09-30 修訂收斂時全套 182 單元＋92 整合＋206 功能綠。
- **原 Spec Kit 產出**：plan、research、data-model、quickstart、tasks、contracts（auth、users）、checklists（requirements、comprehensive）已併入本檔後移除；原始檔可由 `git show c8e5872:"specs/其他/使用者管理與登入/tasks.md"` 等方式追溯。
- **TDD 紅燈證據**（2026-10-06 依憲章 2.1.0 補記；紅燈 commit 只含當時會失敗的測試，可用 `git show <commit>` 查看）：2026-10-01 資料表規範對齊修訂的紅燈 commit：`33a090f`（與 SPEC-002 共用，T096）。初版實作沒有單獨提交紅燈 commit，無紀錄可補。
- **2026-10-08 需求變更（密碼雜湊改 PBKDF2）**：分支 `feature/004-pbkdf2-password`，決策見 [ADR-009](ADR-009-password-hashing-pbkdf2.md)。
  - **實作**：新增 `Pbkdf2PasswordHasher`（取代 `Sha256PasswordHasher`，PBKDF2-HMAC-SHA512、100,000 次迭代，雜湊字串帶 `pbkdf2-sha512$` 前綴，舊 SHA-256 雜湊仍可驗證）；`IPasswordHasher.NeedsRehash`；`User.UpgradePasswordHash`（不動 `PasswordChangedAt`）；`LoginHandler` 登入成功後轉換舊雜湊（另取追蹤中的實體只改雜湊欄位，寫入失敗記 `[PASSWORD_REHASH]` Warning、不影響登入）。資料表結構不變，無 migration。
  - **TDD 紅燈證據**（本機驗證，未提交）：先以空殼讓測試可編譯——`UpgradePasswordHash_keeps_password_changed_at`（雜湊未換）、`Hash_uses_pbkdf2_format`、`Needs_rehash_only_for_legacy_or_weaker_hash`、`Legacy_hash_is_upgraded_on_login`（Expected start "pbkdf2-sha512$"）共 4 案失敗。
  - **BDD**：本次變動的 AC-13、AC-31 為雜湊格式與儲存內容，不適合以 Gherkin 描述，照 docs/TESTING.md 以整合與功能測試表達。
  - **測試數**：建置 0 錯誤 0 警告；單元 308（+1）、整合 142（+5）、功能 305（+2）全綠。
  - **驗證（verify，2026-10-08）**：PASS。AC-13、AC-31 皆有程式與測試對應且通過；其餘 AC 隨全套測試通過，無回歸。
  - **合併**：PR #5，squash commit `a048bcb`（2026-10-08）；規格狀態維持 Archived。
- **2026-10-08 需求變更（登入端點限速）**：分支 `feature/004-login-rate-limit`，延後事項 DEF-017。
  - **實作**：`Configurations/RateLimitConfigs.cs`（`LoginRateLimitOptions` 預設 20 次／60 秒、啟動時驗證；ASP.NET Core 內建 `AddRateLimiter` 依 `RemoteIpAddress` 分區的固定時間窗；`OnRejected` 回 429 ProblemDetails＋`Retry-After` 並記 `[LOGIN_RATE_LIMITED]`）；`Program.cs` 加 `UseRateLimiter()`；`LoginEndpoint` 掛 `RequireRateLimiting`；`appsettings.json` 加 `LoginRateLimit`。不新增套件。
  - **測試工廠**：TestServer 連線沒有 IP，所有登入會算成同一來源，三個既有工廠以設定把上限放寬到 100000；`LoginRateLimitFactory` 把上限設為 3，並以 `X-Test-Client-Ip` 標頭模擬不同來源 IP。
  - **TDD 紅燈證據**（本機驗證，未提交）：`Exceeding_limit_returns_429_with_retry_after`、`Rejected_requests_do_not_count_toward_lockout` 失敗（Expected: TooManyRequests，Actual: Unauthorized）；`Limit_is_per_client_ip` 未實作前即通過，屬回歸護欄。
  - **BDD**：AC-32 需要模擬來源 IP 與計數時間窗，以功能測試表達（docs/TESTING.md §4.6）。
  - **連帶修正**：SPEC-059 測試 `Update_not_found` 原以「內容含『不存在』」計數修改失敗紀錄，同類別另一支測試（隸屬分行不存在）也會寫入這種紀錄，執行順序一變就失敗；改為比較請求前後的修改失敗筆數。
  - **審查（code-review，2026-10-08）**：無 BLOCKING。IMPORTANT 1 項（IIS 前有代理時全行共用額度）屬部署架構，列入 Risks 與 `docs/INFRASTRUCTURE.md` 部署前確認事項；SUGGESTION 採納：開發環境 CORS 開放 `Retry-After` 標頭、`Retry-After` 改稱「建議等待秒數」、IPv6 輪換與日誌量列為已知限制。
  - **測試數**：建置 0 錯誤 0 警告；單元 321、整合 156、功能 322（+3）全綠。
  - **驗證（verify，2026-10-08）**：PASS。AC-32 有程式與測試對應且通過；其餘 AC 隨全套測試通過，無回歸。
  - **合併**：PR #15，squash commit `31f8504`（2026-10-08）；規格狀態維持 Archived。
- **2026-10-08 需求變更（登入改 JWT）**：分支 `feature/004-jwt-auth`，決策見 [ADR-011](ADR-011-jwt-bearer-authentication.md)。
  - **實作**：Core 新增 `SessionAggregate`（`UserSession` [NotAudited]、`UserSessionPublicId`、`RefreshTokenValue`、`UserSessionByTokenHashSpec`／`ByPublicIdSpec`）；`UserByIdWithRolesSpec`。Infrastructure 新增 `UserSessionConfig` 與 migration `20261008135754_UserSessions`（`SysUserSession`）。UseCases：`LoginHandler` 登入成功時建立 session；新增 `RefreshSessionHandler`（輪替、30 秒寬限、重用即撤銷、帳號失效即撤銷）、`LogoutHandler`、`IsSessionActiveHandler`。Web：`JwtOptions`／`JwtSigningKey`（非 Development 未設定 `Jwt:SigningKey` 即無法啟動）、`AccessTokenIssuer`、`RefreshCookie`；`AuthConfigs` 由 Cookie 改為 JwtBearer，`OnTokenValidated` 檢查 session 並依資料庫重建角色與可見選單；`POST /auth/login` 回傳 token、新增 `POST /auth/refresh`、`POST /auth/logout` 改為匿名。新增套件 `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.12（MIT，`THIRD-PARTY-NOTICES.md`）。
  - **TDD 紅燈證據**（本機驗證，未提交）：`UserSessionTests` 5 個以會丟 `NotImplementedException` 的空殼失敗；`UserSessionSchemaTests` 在 migration 加入前因模型有未套用變更而失敗；`JwtAuthTests`／`AuthEndpointsTests` 13 個失敗——登入回應沒有 `tokenType`（Expected "Bearer"、Actual null）、沒有 refresh Cookie、未登入登出回 401、沒有 `/auth/refresh`。
  - **既有測試調整**：新增 `AuthTestSupport.PostLoginAsync`（登入後帶 Bearer）；`ProfileTestSupport.LoginAsync`、`MyMenuContractTests`、`MenuClaimsRevalidationTests`、`SessionRevalidationTests`、`LoggingRedactionTests` 改用之；`AuthWebApplicationFactory` 設定固定測試簽章金鑰；登入契約快照加入 `accessToken`（佔位）、`tokenType`、`expiresIn`。
  - **BDD**：AC-24、AC-33～AC-38 需要操作 Cookie、資料庫時間與自行產生的 token，以功能測試表達；AC-39 為資料表結構，以整合測試表達（docs/TESTING.md §4.6）。AC-24 的非 Development `Secure` 由 Code Review 確認（`RefreshCookie.Options`）。
  - **審查（security review，2026-10-08）**：無 BLOCKING。IMPORTANT 2 項已修正——（1）驗證時只依帳號名稱對應使用者，帳號刪除後重建同名帳號，舊 token 會帶著新帳號權限通過：`IsSessionActiveQuery` 改為一併比對 token 的 `sub` 與 session 的使用者（AC-40）；（2）同一個 refresh token 同時換新兩次，後寫入者覆蓋先寫入者，瀏覽器可能留下失效的 Cookie：新增 `ISessionRotationStore`（Infrastructure `SessionRotationStore` 以 `ExecuteUpdate … WHERE TokenHash = @原值` 條件式保存），搶輸者比照寬限（AC-41）。紅燈：`SessionRotationStoreTests` 第二次保存仍回 true、`Access_token_rejected_after_account_recreated` 回 200；`RefreshSessionHandlerTests` 因用例尚無輪替保存參數而無法編譯。SUGGESTION 採納：每請求驗證改用不追蹤的 `UserSessionByPublicIdForReadSpec`；`SysUserSession` 不清除舊列登記為延後事項 DEF-022。未採納：refresh Cookie 改 `Path=/auth`（前端經 `/api` 代理時路徑不一致，維持 `/`）；開發環境 CORS 允許任意來源（僅開發環境，既有取捨）；session 的 CreateUser 記為 anonymous（登入、換新為匿名端點，與其他匿名寫入一致）。
  - **測試數**：建置 0 錯誤 0 警告；單元 328（+7）、整合 160（+4）、功能 334（+12）全綠。
  - **驗證（verify，2026-10-08）**：PASS。AC-2、AC-5、AC-24、AC-33～AC-39 皆有程式與測試對應且通過；其餘 AC 隨全套測試通過，無回歸。
  - **合併**：PR #17，squash commit `3852544`（2026-10-08）；規格狀態維持 Archived。
