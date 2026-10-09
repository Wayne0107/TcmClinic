# [SPEC-013] Feature: 個人資料維護

<!-- spec-type: feature -->

| 項目 | 內容 |
|------|------|
| SPEC-ID | SPEC-013 |
| 規格類型 | feature |
| 狀態 | Archived |
| 選單 | 管理業務 › 系統管理作業 › 個人資料維護（`personalData`） |
| 建立日期 | 2026-09-29 |
| 最後更新 | 2026-10-04（由 Spec Kit 格式轉為 UDS SDD 格式；內容未變） |
| 原分支 | `013-personal-data`（2026-09-30 修訂：`013-personal-data-standard-columns`） |
| 相關規格 | SPEC-004 使用者管理與登入（擴充 SysUser 電話／Email／密碼修改時間；⚠️ 本規格 FR-008a 修改其 FR-013 密碼規則）、SPEC-005 選單導向授權模型（`MenuAccess:personalData`）、SPEC-006 資料表異動紀錄（密碼遮罩）、SPEC-007 CHB選單與角色種子（補 `personalData`）、SPEC-010 資料訪問日誌（R14 修正後記錄真實帳號）、SPEC-011 受理開狀申請書（⚠️ 2026-09-30 修訂改其 `CurrentUserBank`）、SPEC-014 分行資料維護（分行名稱；⚠️ 2026-09-30 修訂改其 FR-019(a)）、SPEC-016 系統參數維護（沿用 `SysUserLog`；密碼效期維持部署設定，不納入）；SPEC-058 帳號維護（帳號狀態、分行指派）；← SPEC-060 使用者存取記錄（查詢本規格建立的 `SysUserLog`） |

> 📦 本規格隨範本沿用自源專案（CHB），已實作於骨架。內文提到的分行、企業、開狀申請、系統參數、CHB 選單／角色等屬源專案業務，範本已移除；以骨架程式碼與測試為準。另：範本已移除「所屬分行」欄位（`SysUser.BranchId` 與回應中的分行資訊），內文關於分行的需求與設計不再適用。

> **狀態沿革**：Implemented（2026-09-29）；2026-09-30 修訂已實作（資料表規範對齊，見 Clarifications Session 2026-09-30）。
>
> ⚠️ **被後續變更影響的註記**：
> - 2026-10-02 舊後台選單（`/admin/users` 等）移除：`POST /users`、`PUT /users/{id}/password` 的授權改為 `MenuAccess:accountMaintenance`（帳號維護）。設計決策 R1 替代方案中提到的 `MenuAccess:/admin/users` 為當時狀態。
> - 2026-09-30 修訂時改寫的 `CurrentUserBank`（設計決策 R15）其後已由 SPEC-011 受理開狀申請書的收斂任務 T083–T084（commit `3714e04`，機構關聯改 ID 外鍵）移除，現行程式碼中已無此類別。
> - SPEC-016 系統參數維護決定「密碼效期」不納入系統參數，維持部署設定 `PasswordPolicy:ValidDays`（本規格 FR-011「可由系統設定調整」、Assumptions「日後由系統參數維護管理」據此以部署設定實現）。

## Overview（概述）

讓已登入使用者自行檢視並修改**自己的**個人資料（使用者名稱、聯絡電話、電子郵件），並可在同一次修改中變更登入密碼（須驗證目前密碼）；同時唯讀顯示帳號、所屬分行、所屬角色、上次修改密碼時間與密碼尚餘有效天數、帳號狀態。功能依舊系統 eISsuei「管理作業 › 系統管理作業 › 個人資料維護」反推，並修正其兩個缺陷（以表單 `userId` 決定修改對象、確認時未再驗證）。密碼強度規則提升為 12 碼＋大寫＋小寫＋數字並全系統統一。範圍為**後端**：提供 `GET /profile`、`PUT /profile` 與 append-only 操作紀錄 `SysUserLog`，作為前端（另一團隊維護）串接的契約來源。

## Motivation（動機）

舊系統（eISsuei）的「個人資料維護」讓**已登入的使用者自行檢視並修改自己的基本資料**：修改使用者名稱、聯絡電話、電子郵件，並可順便變更自己的登入密碼；同時唯讀顯示帳號、所屬分行、所屬角色、上次修改密碼時間（含密碼尚餘有效天數）與帳號狀態。畫面流程為「編輯 → 預覽 → 確認（或確認並列印）→ 結果訊息」。

新系統 CHB.Web 已有選單項（管理業務 › 系統管理作業 › 個人資料維護，value `personalData`），但頁面仍為空殼、後端尚無對應功能。

各使用者故事的優先理由：

- **US1 檢視（P1）**：這是此頁的前提。沒有正確的個人資料，使用者無從確認自身狀態，也無法進行任何修改。
- **US2 修改基本資料（P2）**：維護聯絡資訊是此頁最常見的用途；排在檢視之後。
- **US3 變更密碼（P3）**：密碼效期到期前使用者需自行換密碼，屬必要安全功能；但依附於 US2 的修改流程，故列 P3。
- **US4 密碼有效期限（P3）**：屬 US1 的延伸顯示資訊，但需要新系統開始記錄「密碼修改時間」與「密碼效期天數」，故獨立列出。

### 舊系統行為對照（反推自 `UserDataAction`、`personalUserSetting*.jsp`）

| 項目 | 舊系統行為 | 本規格處置 |
|------|-----------|-----------|
| 顯示欄位 | 帳號、＊使用者名稱、所屬分行代碼／名稱、所屬角色、新密碼、新密碼確認、聯絡電話、電子郵件、上次修改密碼時間（密碼尚餘 N 天有效）、狀態 | 全數保留（FR-001） |
| 可修改欄位 | 使用者名稱（必填，≤30）、聯絡電話（≤20）、電子郵件（≤100）、新密碼（選填，空白＝不變更） | 保留並補格式驗證；使用者名稱上限改為 100，與使用者管理一致（FR-003～FR-006） |
| 密碼規則 | ≥12 碼，須同時含大寫、小寫、數字；不可與目前密碼相同 | 沿用舊規則並**全系統統一**（FR-008；改「使用者管理與登入」FR-013） |
| 修改密碼 | 不需輸入目前密碼 | **強化**：須輸入並驗證目前密碼（FR-009） |
| 修改對象 | 以表單隱藏欄位 `userId` 決定修改誰（**可竄改修改他人資料的漏洞**） | **修正**：一律以登入身分決定，忽略任何請求中的使用者識別（FR-002） |
| 驗證位置 | 密碼規則僅於「預覽」檢查，「確認」時未再驗證即寫入 | **修正**：儲存時伺服器端完整驗證（FR-007） |
| 密碼效期 | 預設 90 天（系統設定），顯示剩餘天數 | 保留顯示（FR-011）；逾期時的登入處置不在本功能範圍（FR-012） |
| 變更密碼副作用 | 更新密碼修改時間、清除登入錯誤次數 | 更新密碼修改時間（FR-010） |
| 操作紀錄 | 寫入使用者操作日誌（功能 `PersonalUserSetting`／動作 `Modify`，成功與失敗皆記） | 保留（FR-015） |
| 預覽／列印 | 預覽頁唯讀呈現、密碼以 `****` 遮蔽；「確認並列印」附列印人員／分行／時間 | 屬前端呈現，後端不另提供（見 Assumptions） |

## Clarifications（釐清紀錄）

### Session 2026-09-29

- Q: 密碼強度規則要採舊系統（≥12 碼＋大寫＋小寫＋數字）還是新系統現行（8～128 碼＋英文字母＋數字）？ → A: 採舊系統規則並**全系統統一**：12～128 碼，且同時包含大寫字母、小寫字母、數字。新增使用者、管理者重設密碼、個人變更密碼一律適用；此決策**修改「使用者管理與登入」FR-013**。
- Q: 變更密碼時是否要求輸入並驗證目前密碼？ → A: 要。填寫新密碼時必須同時提供目前密碼，伺服器端驗證正確後才允許變更（強化舊系統行為，前端需新增「目前密碼」欄位）。
- Q: 密碼逾期的處置範圍？ → A: 本功能只負責顯示剩餘天數與逾期狀態；逾期時登入的處置（強制改密碼等）不在本規格範圍，另開規格處理。

### Session 2026-09-30

背景：DATABASE-SCHEMA 修訂為「跨表關聯一律用 `Id`」「所有表（含 append-only 日誌表）統一五個標準欄位」，本功能既有設計兩處不符（analyze C1、C2）。

- Q: 使用者所屬分行改為以分行識別（ID 外鍵）關聯時，範圍只改使用者，還是連同受理作業（原 011）的申請分行欄位一起改？ → A: **只改使用者**。受理作業仍以機構代碼比對，改由使用者所屬分行組回機構代碼，其比對邏輯不動，留待該規格處理。
- Q: 個人資料回傳的分行代碼是 4 碼分行代碼還是 7 碼完整機構代碼？ → A: **4 碼分行代碼**（與分行主檔一致；銀行代碼全系統相同）。未歸屬分行時分行代碼與名稱皆為空值。
- Q: 既有使用者的機構代碼資料是否轉換為分行關聯？ → A: **不轉換**。目前尚無正式環境資料，唯一有值者為開發種子，由種子重新指派示範分行。
- 操作紀錄（FR-015）改用系統統一的五個標準欄位記錄操作者與時間，不另設自訂欄位；外部可觀察行為不變。

## Requirements（需求）

### Requirement: US1 檢視自己的個人資料（P1）

The system SHALL 讓已登入使用者查詢**本人**的帳號、使用者名稱、所屬分行代碼與名稱、所屬角色、聯絡電話、電子郵件、上次修改密碼日期與密碼尚餘有效天數、帳號狀態，且回應不含任何密碼資訊。

已登入的使用者進入「個人資料維護」頁即看到上述資料。獨立驗證方式：以某一已知使用者登入後查詢，回傳欄位與資料庫一致且不含任何密碼資訊。涵蓋 FR-001、FR-002、FR-013、FR-014。

#### Scenario: AC-1 回傳登入者本人資料
- **GIVEN** 使用者已登入
- **WHEN** 查詢個人資料
- **THEN** 回傳的是**登入者本人**的帳號、使用者名稱、所屬分行代碼／名稱、所屬角色（可多個）、聯絡電話、電子郵件、上次修改密碼時間、密碼尚餘有效天數與狀態

#### Scenario: AC-2 不含密碼資訊
- **GIVEN** 使用者已登入
- **WHEN** 查詢個人資料
- **THEN** 回傳內容不含密碼明文、密碼雜湊或 salt

#### Scenario: AC-3 未填聯絡資料回傳空值
- **GIVEN** 使用者從未填過聯絡電話與電子郵件
- **WHEN** 查詢個人資料
- **THEN** 這兩個欄位回傳為空值，其餘欄位正常

#### Scenario: AC-4 未登入拒絕
- **GIVEN** 未登入的存取者
- **WHEN** 查詢個人資料
- **THEN** 系統拒絕存取

### Requirement: US2 修改自己的基本資料（P2）

The system SHALL 讓使用者修改自己的使用者名稱、聯絡電話、電子郵件（不變更密碼），於伺服器端完整驗證後整筆儲存，並回報「個人資料維護 (帳號) [使用者名稱] 修改成功」。

獨立驗證方式：送出一筆只含基本資料、密碼留空的修改，資料更新、密碼不變（仍可用原密碼登入）。涵蓋 FR-002～FR-007、FR-015、FR-016。

#### Scenario: AC-5 修改基本資料成功
- **GIVEN** 使用者已登入
- **WHEN** 送出合法的使用者名稱、聯絡電話、電子郵件且新密碼留空
- **THEN** 系統更新這三個欄位、密碼維持不變，並回報修改成功訊息

#### Scenario: AC-6 使用者名稱空白被拒
- **GIVEN** 使用者名稱為空白（或僅含空白字元）
- **WHEN** 送出
- **THEN** 系統拒絕並回報「使用者名稱不可為空白」，不寫入任何資料

#### Scenario: AC-7 電子郵件格式錯誤被拒
- **GIVEN** 電子郵件格式不正確
- **WHEN** 送出
- **THEN** 系統拒絕並回報欄位層級錯誤訊息

#### Scenario: AC-8 夾帶他人識別碼仍只改本人
- **GIVEN** 請求中夾帶其他使用者的識別碼
- **WHEN** 送出
- **THEN** 系統仍只修改登入者本人，他人資料不受影響

### Requirement: US3 變更自己的登入密碼（P3）

The system SHALL 在使用者於同一次修改中提供正確的目前密碼與符合規則、兩次一致的新密碼時，以新密碼取代舊密碼並更新「上次修改密碼時間」，使密碼有效天數重新起算；任何密碼檢查失敗時整筆拒絕（含基本資料）。

獨立驗證方式：送出含合法新密碼的修改，之後只能以新密碼登入、舊密碼失效、上次修改密碼時間為今日、剩餘天數重置為完整效期。涵蓋 FR-008、FR-008a、FR-009、FR-010。

#### Scenario: AC-9 變更密碼成功
- **GIVEN** 使用者已登入
- **WHEN** 送出正確的目前密碼，以及符合規則且兩次輸入一致的新密碼
- **THEN** 系統以新密碼覆寫、舊密碼立即失效、上次修改密碼時間更新為當下

#### Scenario: AC-10 兩次新密碼不一致
- **GIVEN** 新密碼與新密碼確認不一致
- **WHEN** 送出
- **THEN** 系統拒絕並回報「新密碼與新密碼確認內容不一致」，不寫入任何資料（包含基本資料）

#### Scenario: AC-11 只填其中一個密碼欄位
- **GIVEN** 只填新密碼或只填新密碼確認其中之一
- **WHEN** 送出
- **THEN** 系統拒絕並回報另一欄不可為空白

#### Scenario: AC-12 新密碼不符強度規則
- **GIVEN** 新密碼少於 12 碼，或未同時包含大寫字母、小寫字母、數字
- **WHEN** 送出
- **THEN** 系統拒絕並回報具體未符合的規則

#### Scenario: AC-13 新密碼與目前密碼相同
- **GIVEN** 新密碼與目前密碼相同
- **WHEN** 送出
- **THEN** 系統拒絕並回報「密碼不可與上次相同」

#### Scenario: AC-14 未填目前密碼
- **GIVEN** 填了新密碼但未填目前密碼
- **WHEN** 送出
- **THEN** 系統拒絕並回報「目前密碼不可為空白」，不寫入任何資料

#### Scenario: AC-15 目前密碼錯誤
- **GIVEN** 目前密碼輸入錯誤
- **WHEN** 送出
- **THEN** 系統拒絕並回報「目前密碼不正確」，不寫入任何資料（包含基本資料），並記錄一筆修改失敗的操作紀錄

#### Scenario: AC-16 不改密碼時不需目前密碼
- **GIVEN** 新密碼留空（只改基本資料）
- **WHEN** 送出
- **THEN** 不需要輸入目前密碼

### Requirement: US4 查看密碼有效期限（P3）

The system SHALL 依「上次修改密碼時間」與密碼效期天數計算並回傳「密碼尚餘有效天數」與是否已逾期，僅供顯示、不限制使用。

使用者在個人資料中看到上次修改密碼的日期與「密碼尚餘 N 天有效」，提醒自己在到期前變更密碼。獨立驗證方式：設定某使用者上次修改密碼日期為已知日期，查詢個人資料，剩餘天數依效期規則正確計算。涵蓋 FR-010、FR-011、FR-012。

#### Scenario: AC-17 當日修改剩餘完整效期
- **GIVEN** 密碼效期為 90 天、使用者今天剛修改密碼
- **WHEN** 查詢個人資料
- **THEN** 剩餘有效天數為 90 天

#### Scenario: AC-18 31 天前修改剩 59 天
- **GIVEN** 密碼效期為 90 天、使用者 31 天前修改密碼
- **WHEN** 查詢個人資料
- **THEN** 剩餘有效天數為 59 天

#### Scenario: AC-19 已逾期僅顯示
- **GIVEN** 使用者密碼已超過效期
- **WHEN** 查詢個人資料
- **THEN** 剩餘有效天數為 0，並可辨識為「已逾期」（僅顯示，不限制登入或操作）

### 功能需求條文（Functional Requirements）

**檢視（US1、US4）**

- **FR-001**: 系統 MUST 提供「查詢本人個人資料」功能，回傳：帳號、使用者名稱、所屬分行代碼（4 碼）、所屬分行名稱（兩者皆由使用者所屬分行的關聯自分行主檔取得）、所屬角色（角色代碼與名稱清單，可多個；無角色時為空清單）、聯絡電話、電子郵件、上次修改密碼時間、密碼尚餘有效天數、是否已逾期、帳號狀態。MUST NOT 回傳密碼明文、密碼雜湊或 salt。
- **FR-002**: 查詢與修改的對象 MUST 一律為**目前登入者本人**，由伺服器端依登入身分決定；請求中任何使用者識別資訊 MUST 被忽略，不得據以讀取或修改他人資料（修正舊系統以表單 `userId` 決定對象之漏洞）。

**修改基本資料（US2）**

- **FR-003**: 系統 MUST 允許使用者修改自己的使用者名稱、聯絡電話、電子郵件；帳號、所屬分行、所屬角色、帳號狀態 MUST NOT 可由本功能修改。
- **FR-004**: 使用者名稱 MUST 為必填（去除前後空白後不得為空）且長度不超過 100 字元。（設計階段決議：與「使用者管理」既有規則一致；舊系統為 30，若沿用會使管理者設定的較長名稱無法儲存，見設計決策 R7。）
- **FR-005**: 聯絡電話為選填，長度不超過 20 字元，僅允許數字、空白、`+`、`-`、`(`、`)`、`#`（分機）。
- **FR-006**: 電子郵件為選填，長度不超過 100 字元；有填寫時 MUST 符合一般電子郵件格式。
- **FR-007**: 所有欄位驗證 MUST 於伺服器端在儲存時完整執行（不得僅依賴前端或預覽步驟）；任何一項未通過時 MUST 整筆拒絕、不寫入任何資料，並回報欄位層級錯誤訊息。

**變更密碼（US3）**

- **FR-008**: 新密碼為選填，留空代表不變更密碼；有填寫時 MUST 同時提供新密碼確認且兩者一致，並 MUST 符合**全系統統一的密碼強度規則**：長度 12～128 碼，且同時包含英文大寫字母（A–Z）、英文小寫字母（a–z）、數字（0–9）。新密碼 MUST NOT 與目前密碼相同。
- **FR-008a**: 上述密碼強度規則 MUST 同樣適用於「使用者管理與登入」的新增使用者與管理者重設密碼（**修改該規格 FR-013**：由「8～128 碼、英文字母＋數字」改為本條規則）。既有密碼不追溯，不強制變更。
- **FR-009**: 有填寫新密碼時，使用者 MUST 同時提供目前密碼；伺服器端 MUST 驗證目前密碼正確後才允許變更，未提供或不正確時 MUST 整筆拒絕（含基本資料）。新密碼留空時 MUST NOT 要求目前密碼。
- **FR-010**: 密碼變更成功後，系統 MUST 以新密碼的加鹽雜湊覆寫舊密碼（舊密碼立即失效），並 MUST 將「上次修改密碼時間」更新為當下時間；密碼有效天數 MUST 自此重新起算。

**密碼效期（US4）**

- **FR-011**: 系統 MUST 記錄每位使用者的「上次修改密碼時間」，並依「密碼效期天數」（預設 90 天，可由系統設定調整）計算「密碼尚餘有效天數」：以上次修改密碼日為第 1 天，剩餘天數＝效期天數 −（今日 − 上次修改密碼日）之天數，最小為 0；為 0 時標示為已逾期。無修改紀錄之既有帳號以建立時間代替。所有使用者建立或管理者重設密碼時 MUST 同樣更新此時間。
- **FR-012**: 本功能對密碼逾期僅負責**顯示**剩餘天數與逾期狀態；MUST NOT 因逾期而限制登入或本功能的使用。逾期時登入的處置（例如強制先變更密碼）不在本規格範圍，另開規格處理。

**帳號狀態**

- **FR-013**: 系統 MUST 回傳使用者的帳號狀態（啟用／停用），供畫面顯示；停用者畫面 MUST 能提示「請洽管理人員啟用方才可以操作」。狀態的維護（啟用／停用）不在本功能範圍，由「帳號維護」負責；本功能上線時若尚無停用機制，所有可登入的帳號一律視為「啟用」。

**授權、稽核與契約**

- **FR-014**: 個人資料的查詢與修改 MUST 在伺服器端受「個人資料維護」選單（value `personalData`）之授權控管，未授權或未登入者一律拒絕；系統 MUST 於選單種子補上 value 為 `personalData` 的「管理業務 › 系統管理作業 › 個人資料維護」選單項，並授予 CHB 五個角色（BH 經辦、BS 主管、SM 系統管理員、MB 總行、BM 分行管理員）可見。
- **FR-015**: 每次修改（成功或失敗）MUST 留下操作紀錄，內容至少包含：功能（個人資料維護）、動作（修改）、結果（成功／失敗）、操作者帳號與使用者名稱、時間；密碼相關內容 MUST NOT 以任何形式出現在操作紀錄或資料異動紀錄中（沿用「資料表異動紀錄」之敏感欄位遮罩）。（設計階段決議：「一次修改」指通過請求格式驗證、進入業務處理的請求；格式錯誤被直接拒絕的請求不記，見設計決策 R8。）
- **FR-016**: 修改成功時，系統 MUST 回報可直接顯示給使用者的結果訊息，格式對齊舊系統：「個人資料維護 (帳號) [使用者名稱] 修改成功」；失敗時回報「……修改失敗」或欄位層級錯誤。
- **FR-017**: 對外契約（欄位語意、錯誤形狀、狀態語意）MUST 與系統既有 API 慣例一致，並以 CHB.Web「個人資料維護」頁所需資料為對齊目標。

### 成功準則（Success Criteria）

- **SC-001**: 使用者可在 1 分鐘內完成「檢視 → 修改聯絡資料 → 儲存」的完整流程。
- **SC-002**: 以任何方式（竄改請求內容、夾帶他人識別碼）嘗試讀取或修改他人個人資料，成功率為 0%。
- **SC-003**: 所有不合規的輸入（使用者名稱空白、格式錯誤、密碼不符規則、兩次密碼不一致、與目前密碼相同、目前密碼未填或錯誤）100% 被伺服器端拒絕且不留任何部分寫入。
- **SC-003a**: 新增使用者、管理者重設密碼、個人變更密碼三個入口，對同一組不合規密碼（如 11 碼、缺大寫、缺小寫、缺數字）的拒絕結果 100% 一致。
- **SC-004**: 變更密碼成功後，舊密碼登入 100% 失敗、新密碼登入 100% 成功，且密碼尚餘有效天數重置為完整效期。
- **SC-005**: 密碼尚餘有效天數的計算結果與舊系統在相同日期條件下一致（例：效期 90 天、31 天前修改 → 59 天）。
- **SC-006**: 查詢回應、操作紀錄與資料異動紀錄中出現密碼明文、雜湊或 salt 的比例為 0%。
- **SC-007**: 每一次進入業務處理的修改（成功或失敗）100% 有對應的操作紀錄可查；格式驗證即被拒絕的請求不在此列（同 FR-015 定義）。

### 邊界情境（Edge Cases）

- 請求攜帶他人的使用者識別碼（舊系統漏洞情境）：一律忽略，只處理登入者本人（FR-002）。
- 只填目前密碼、新密碼留空：視為不變更密碼，目前密碼忽略、不驗證。
- 已登入者反覆以錯誤的目前密碼嘗試變更（疑似工作階段被盜用後猜密碼）：每次失敗皆拒絕並記錄操作紀錄，且計入與登入共用的失敗鎖定計數（連續 5 次鎖定約 15 分鐘；設計階段決議，見設計決策 R4）。
- 本功能上線前以舊規則（8 碼）設定的既有密碼：仍可正常登入，不強制變更；下次變更或重設時才需符合新規則。
- 新密碼欄位僅含空白字元：視為有填寫、依密碼規則驗證（不自動去除空白後當作「未填」，避免使用者誤以為已變更密碼）。
- 基本資料合法但新密碼不合法：整筆拒絕，不得「部分成功」（基本資料已更新但密碼沒改）。
- 使用者名稱、聯絡電話、電子郵件前後空白：儲存前去除前後空白；去除後使用者名稱為空則拒絕。
- 聯絡電話或電子郵件送空值：視為清除該欄位（兩者皆為選填）。
- 既有帳號（本功能上線前建立者）沒有密碼修改時間紀錄：以該帳號的建立時間視為上次修改密碼時間計算剩餘天數。
- 使用者在修改期間被管理者（帳號維護）停用或刪除：已刪除者不得修改；登入工作階段的有效性依既有登入機制處理。
- 使用者未歸屬任何分行：分行代碼與分行名稱皆回傳空值，不視為錯誤。
- 使用者名稱（100）、電子郵件（100）、聯絡電話（20）超過長度上限：拒絕並回報欄位層級錯誤訊息。
- 修改失敗（例如儲存時發生非預期錯誤）：不得留下部分寫入，並記錄一筆「修改失敗」的操作紀錄。

### 關鍵實體（Key Entities）

- **使用者（既有，需擴充）**：登入者本人。既有屬性：帳號、使用者名稱、密碼（加鹽雜湊）、所屬角色。本功能需新增：**聯絡電話**（選填）、**電子郵件**（選填）、**上次修改密碼時間**；並將所屬機構代碼改為**所屬分行**（以分行識別參照分行主檔，可空＝未歸屬）。所屬分行的指派由「帳號維護」規格負責，本功能僅讀取。帳號狀態（啟用／停用）若尚未存在，由「帳號維護」規格引入，本功能僅讀取。
- **分行（外部參照）**：使用者以分行識別參照分行主檔，分行代碼與名稱皆由主檔取得，供顯示用；分行主檔由「分行資料維護」規格負責，本功能僅讀取。
- **角色（既有）**：使用者所屬的一或多個角色，本功能僅顯示角色名稱。
- **密碼效期設定（系統參數）**：密碼有效天數，預設 90 天；日後由「系統參數維護」管理。
- **操作紀錄**：每次修改的結果紀錄（功能、動作、結果、操作者、時間），不含密碼內容。

### 假設（Assumptions）

- **預覽與列印屬前端呈現**：舊系統的「預覽」頁（唯讀呈現、密碼以 `****` 遮蔽）與「確認並列印」（列印人員／分行／列印時間）由前端以既有查詢資料組成；後端不提供獨立的預覽或列印功能。所有驗證於儲存時由後端執行（FR-007），前端可自行做即時提示。
- **「重新輸入」按鈕**：屬前端表單重設，不涉及後端。
- **並行修改**：個人資料僅本人修改，與管理者「帳號維護」同時修改同一人的機率極低，本功能不要求版本權杖；後寫入者生效。
- **登入工作階段**：變更密碼後，目前的登入工作階段維持有效；強制登出其他裝置的工作階段不在本次範圍。
- **登入失敗鎖定**：「目前密碼」輸入錯誤併入與登入共用的失敗鎖定計數（見「使用者管理與登入」FR-006a）；鎖定期間變更密碼一律拒絕；變更密碼成功後計數歸零（對應舊系統變更密碼時清除登入錯誤次數）。（設計階段決議，見設計決策 R4。）
- **前端配合**：因 FR-009，前端「個人資料維護」頁需新增「目前密碼」欄位（與舊畫面不同）；新增使用者／重設密碼的前端提示文字需改為 12 碼規則。
- **密碼效期設定**：「系統參數維護」尚未實作前，以預設 90 天為準，並保留可調整的設定來源。
- **分行名稱來源**：分行代碼與名稱由「分行資料維護」的分行主檔提供；使用者未歸屬分行時為空，不影響其他欄位。
- **操作紀錄的查詢畫面**：本功能只負責寫入；查詢由「使用者存取記錄」規格負責。
- **依賴既有規格**：沿用「使用者管理與登入」的使用者與加鹽雜湊密碼機制（並修改其 FR-013 密碼規則，見 FR-008a）、「選單導向授權模型」的選單授權、「資料表異動紀錄」的敏感欄位遮罩、「CHB選單與角色種子」的選單種子機制。

## Acceptance Criteria（驗收條件）

測試檔路徑前綴：功能測試 `backend/tests/TcmClinic.Api.FunctionalTests/`、整合測試 `backend/tests/TcmClinic.Api.IntegrationTests/`、單元測試 `backend/tests/TcmClinic.Api.UnitTests/`。

| AC | 來源 | 條件 | 驗證項 |
|----|------|------|--------|
| AC-1 | US1 情境 1、FR-001、FR-002 | 回傳登入者本人資料；已歸屬分行時 `branchCode` 為 4 碼、`branchName` 取自分行主檔 | `backend/tests/TcmClinic.Api.FunctionalTests/ProfileEndpointsTests.cs` › `Get_returns_own_profile`、`Get_returns_branch_code_and_name_from_master`、`Each_user_sees_only_own_profile` |
| AC-2 | US1 情境 2、FR-001 | 回應欄位恰為契約所列，不含密碼、雜湊、salt | `ProfileEndpointsTests` › `Get_does_not_expose_credentials` |
| AC-3 | US1 情境 3 | 未填的電話與 email 回傳 null | `ProfileEndpointsTests` › `Get_returns_own_profile` |
| AC-4 | US1 情境 4、FR-014 | 未登入 401；無 `personalData` 可見性 403 | `ProfileEndpointsTests` › `Get_requires_login`、`Get_requires_personal_data_menu` |
| AC-5 | US2 情境 1、FR-003、FR-016 | 修改基本資料 200 並回舊系統格式訊息，值去除前後空白，原密碼仍可登入 | `ProfileEndpointsTests` › `Put_updates_basic_data` |
| AC-6 | US2 情境 2、FR-004 | 名稱空白或 101 字 → 400（`displayName`），不寫入 | `ProfileEndpointsTests` › `Put_rejects_invalid_basic_data` |
| AC-7 | US2 情境 3、FR-005、FR-006 | email／電話格式或長度不符 → 400 指出欄位 | `ProfileEndpointsTests` › `Put_rejects_invalid_basic_data`；`backend/tests/TcmClinic.Api.UnitTests/UserAggregate/EmailAddressTests.cs` › `Rejects_malformed`；`backend/tests/TcmClinic.Api.UnitTests/UserAggregate/PhoneNumberTests.cs` › `Rejects_strings_containing_letters` |
| AC-8 | US2 情境 4、SC-002 | 夾帶他人 `id`／`account` 仍只改本人；他人資料不變 | `ProfileEndpointsTests` › `Put_ignores_foreign_identifiers`、`Each_user_sees_only_own_profile` |
| AC-9 | US3 情境 1、SC-004、FR-010 | 變更密碼成功：舊密碼 401、新密碼 200、修改日為今日、剩餘 90 天、記 Success | `backend/tests/TcmClinic.Api.FunctionalTests/ProfilePasswordEndpointsTests.cs` › `Change_password_succeeds` |
| AC-10 | US3 情境 2 | 兩次新密碼不一致 → 400（`confirmNewPassword`），雜湊不變 | `ProfilePasswordEndpointsTests` › `Invalid_password_fields_are_rejected` |
| AC-11 | US3 情境 3 | 只填其中一個密碼欄位 → 400 指出另一欄 | `ProfilePasswordEndpointsTests` › `Invalid_password_fields_are_rejected` |
| AC-12 | US3 情境 4、FR-008 | 11 碼／缺大寫／缺小寫／缺數字／全空白 → 400 並列出未符合規則 | `ProfilePasswordEndpointsTests` › `Invalid_password_fields_are_rejected`；`backend/tests/TcmClinic.Api.UnitTests/UserAggregate/PasswordPolicyTests.cs` › `Length_boundaries`、`Reports_missing_character_class`、`Reports_multiple_failures`、`Rejects_whitespace_only` |
| AC-13 | US3 情境 5 | 新密碼等於目前密碼 → 400「密碼不可與上次相同。」 | `ProfilePasswordEndpointsTests` › `Same_as_current_password_is_rejected` |
| AC-14 | US3 情境 6、FR-009 | 未填目前密碼 → 400（`currentPassword`），不寫入 | `ProfilePasswordEndpointsTests` › `Invalid_password_fields_are_rejected` |
| AC-15 | US3 情境 7、FR-009 | 目前密碼錯誤 → 400，名稱亦不改，記 Failure 操作紀錄 | `ProfilePasswordEndpointsTests` › `Wrong_current_password_is_rejected` |
| AC-16 | US3 情境 8 | 新密碼留空時不需目前密碼；只填目前密碼時忽略 | `ProfileEndpointsTests` › `Put_updates_basic_data`、`Current_password_is_ignored_without_new_password` |
| AC-17 | US4 情境 1 | 當日修改剩餘天數＝效期天數（90） | `ProfileEndpointsTests` › `Get_returns_own_profile`；`backend/tests/TcmClinic.Api.UnitTests/UserAggregate/PasswordExpiryTests.cs` › `Same_day_equals_valid_days` |
| AC-18 | US4 情境 2、SC-005 | 31 天前修改剩 59 天，與舊系統一致 | `backend/tests/TcmClinic.Api.FunctionalTests/ProfileExpiryEndpointsTests.cs` › `Changed_31_days_ago_leaves_59_days`；`PasswordExpiryTests` › `Matches_legacy_examples`、`Result_is_within_bounds` |
| AC-19 | US4 情境 3、FR-012 | 逾期剩 0 天、`passwordExpired=true`，仍可登入與修改 | `ProfileExpiryEndpointsTests` › `Expired_password_is_display_only`；`PasswordExpiryTests` › `Expired_iff_remaining_is_zero` |
| AC-20 | SC-001 | 1 分鐘內完成「檢視 → 修改聯絡資料 → 儲存」 | 手動：【待確認】前端頁面串接後實測計時（手動走查無對應步驟） |
| AC-21 | SC-003、FR-007 | 任一欄位不合規即整筆拒絕，無部分寫入（含「改名＋錯誤密碼」） | `ProfileEndpointsTests` › `Put_is_atomic`；`ProfilePasswordEndpointsTests` › `Wrong_current_password_is_rejected` |
| AC-22 | SC-003a、FR-008a | 新增使用者、管理者重設、本人變更三入口對同一組不合規密碼的拒絕訊息一致；既有密碼不追溯 | `ProfilePasswordEndpointsTests` › `Password_policy_is_consistent_across_entry_points`；`backend/tests/TcmClinic.Api.FunctionalTests/UserCrudEndpointsTests.cs` › `Weak_password_returns_400`、`Weak_password_on_reset_returns_400` |
| AC-23 | SC-006 | 異動紀錄密碼雜湊／salt 為遮罩；日誌與操作紀錄不含密碼字串；命令字串表示不含密碼 | `ProfilePasswordEndpointsTests` › `Change_password_audit_log_masks_credentials`；`backend/tests/TcmClinic.Api.FunctionalTests/LoggingRedactionTests.cs` › `Profile_password_fields_never_logged`；`backend/tests/TcmClinic.Api.UnitTests/Profile/UpdateMyProfileHandlerTests.cs` › `Command_ToString_hides_passwords` |
| AC-24 | SC-007、FR-015 | 進入用例的修改（成功／失敗）各記一筆 `SysUserLog`；格式驗證 400 不記；獨立交易不帶出主交易髒資料 | `ProfileEndpointsTests` › `Put_writes_user_log_and_audit_log`、`Put_rejects_invalid_basic_data`；`backend/tests/TcmClinic.Api.IntegrationTests/Auditing/UserLogRecorderTests.cs` › `Records_row_with_current_user`、`Failure_log_does_not_flush_pending_changes`、`Write_failure_is_swallowed_and_logged` |
| AC-25 | FR-014 | `personalData` 位於 management › system、順序 2；BH／BS／SM／MB／BM 皆可見；種子冪等 | `backend/tests/TcmClinic.Api.IntegrationTests/Data/PersonalDataMenuSeedTests.cs` › `PersonalData_leaf_is_under_management_system`、`All_chb_roles_can_see_personal_data`、`Reseeding_is_idempotent` |
| AC-26 | FR-011 | 新增使用者與重設密碼更新修改時間；無紀錄時以建立日計算；效期天數可設定（60）；設 0 啟動失敗 | `UserCrudEndpointsTests` › `Create_and_reset_record_password_changed_at`；`ProfileExpiryEndpointsTests` › `Missing_changed_at_falls_back_to_create_date`、`Valid_days_setting_is_applied`、`Zero_valid_days_fails_on_start` |
| AC-27 | FR-016、Edge Case（非預期錯誤） | 儲存非預期錯誤 → 500 ProblemDetails、`detail` 為修改失敗訊息、記 Failure、無部分寫入 | `ProfileEndpointsTests` › `Unexpected_save_error_returns_problem_details`；`UpdateMyProfileHandlerTests` › `Unexpected_save_error_is_logged_and_returned_as_critical_error` |
| AC-28 | Edge Case（反覆猜目前密碼）、R4 | 連錯 5 次後鎖定，改密碼與登入皆被拒；成功變更後計數歸零 | `ProfilePasswordEndpointsTests` › `Repeated_wrong_current_password_locks_account`、`Successful_change_resets_failure_count` |
| AC-29 | Edge Case（清除聯絡資料、已刪除者） | 電話／email 送空值即清除；已刪除者 `PUT /profile` → 401 且資料不變 | `ProfileEndpointsTests` › `Put_clears_contact_with_empty_values`、`Deleted_user_cannot_update` |
| AC-30 | FR-017 | `GET /profile` 回應欄位名稱與型別鎖定（契約快照） | `backend/tests/TcmClinic.Api.FunctionalTests/Contracts/ProfileContractTests.cs` › `Profile_response_contract` |
| AC-31 | R12 | 改名後 `GET /auth/me` 的 `displayName` 立即更新 | `ProfileEndpointsTests` › `Rename_is_reflected_in_auth_me` |
| AC-32 | R14 | 已登入請求的稽核欄位、異動紀錄、操作紀錄記錄真實登入帳號；啟動種子仍為 `system` | `backend/tests/TcmClinic.Api.FunctionalTests/CurrentUserResolutionTests.cs` › `Authenticated_request_records_real_account`、`User_log_written_in_separate_scope_records_real_account`、`Seeded_rows_are_created_by_system` |
| AC-33 | R15、Session 2026-09-30 | `SysUser` 以可空 `BranchId` 外鍵關聯分行、無 `BankId`；未歸屬時分行代碼與名稱皆為 null；`SysUserLog` 具五個標準欄位 | `backend/tests/TcmClinic.Api.IntegrationTests/Data/UserPersonalDataSchemaTests.cs` › `SysUser_has_personal_data_columns`、`SysUser_references_branch_by_id`、`SysUserLog_schema_matches_design`、`Phone_and_email_are_not_sensitive`；`ProfileEndpointsTests` › `Get_returns_null_branch_when_unassigned`；`backend/tests/TcmClinic.Api.UnitTests/UserAggregate/UserPersonalDataTests.cs` › `AssignBranch_sets_and_clears` |

> AC-22 的管理者端點授權自 2026-10-02 起為 `MenuAccess:accountMaintenance`（舊後台選單 `/admin/users` 移除），測試已改以 `accountMaintenance` 與 `personalData` 兩個選單授予專用角色。

## Technical Design（技術設計）

### 技術脈絡

- 語言／框架：C# / .NET 9。
- 相依：FastEndpoints（REPR）、FluentValidation（FastEndpoints 內建）、Mediator、Ardalis.Result、Ardalis.Specification、Ardalis.SmartEnum、Vogen、EF Core；無新增外部相依。
- 儲存：SQL Server（正式）／SQLite（開發）；異動 `SysUser`（+3 欄，並以 `BranchId` 外鍵取代 `BankId`，R15）、新增 `SysUserLog`（五個標準欄位）；種子異動 `SysTasks`（選單）、`SysRoleTasks`（角色↔選單可見性）。
- 測試：xUnit；UnitTests（`PasswordPolicy`／`PasswordExpiry` CsCheck 屬性測試、VO、`User` 方法）、IntegrationTests（EF 綱要與欄位說明、Migration、種子、`UserLogRecorder` 獨立交易）、FunctionalTests（`/profile` 契約、既有使用者端點密碼規則、日誌不含密碼）。
- 專案類型：web-service（後端；前端 CHB.Web 由另一團隊依本規格「API 契約」串接）。
- 效能目標：單一使用者單筆讀寫；`GET /profile` 最多 3 次查詢（使用者含角色關聯、角色清單、所屬分行），無 N+1。
- 限制：對象只能是登入者本人（防 IDOR）；整筆原子寫入；密碼明文不得進入日誌、操作紀錄、異動紀錄；既有 `/users` 端點回應形狀不變；文件與註解 zh-TW。
- 規模：4 個使用者故事；1 個聚合擴充、2 個 VO、2 個純函式規則、1 張 append-only 表、2 個新端點、3 個既有端點行為調整。

技術取徑摘要：

1. **新端點** `GET /profile`、`PUT /profile`，路由與 DTO 不含使用者識別，一律以登入身分查本人（R1）；授權 `MenuAccess:personalData`，種子授予 CHB 五角色（R11）。
2. **擴充 `User` 聚合**：新增 `Phone`、`Email`（Vogen 值物件）與 `PasswordChangedAt`；`SetPassword` 簽章加入變更時間，所有設定密碼路徑一併更新（R5、R6）。
3. **密碼規則集中到 Core `PasswordPolicy`**（12～128 碼＋大寫＋小寫＋數字），新增使用者、管理者重設、本人變更三處共用（R2，改 SPEC-004 FR-013）。
4. **目前密碼錯誤併入既有登入鎖定計數**，防止工作階段被盜用後猜密碼（R4）。
5. **新增 append-only `SysUserLog`** 記錄成功／失敗，獨立 DbContext 盡力寫入，作為日後「使用者存取記錄」的資料來源（R8）。
6. 密碼效期以 Core `PasswordExpiry` 純函式計算，天數由設定 `PasswordPolicy:ValidDays`（預設 90）提供（R5）。

### 受影響元件

沿用既有單一後端方案（Core／UseCases／Infrastructure／Web 四層與三個測試專案），不新增專案。新增 `Profile` 用例與端點資料夾，與既有 `Users`（管理者用）分開，使「本人資料」與「管理他人」的授權語意在結構上分離。

| 檔案 | 變更 |
|------|------|
| `backend/src/TcmClinic.Api.Core/UserAggregate/User.cs` | 修改：+`Phone`／`Email`／`PasswordChangedAt`；`SetPassword(hash, salt, changedAt)`；`UpdateContact`；`BankId` → `BranchId?`、`SetBankId` → `AssignBranch`（R15） |
| `backend/src/TcmClinic.Api.Core/UserAggregate/PhoneNumber.cs` | 新增：Vogen VO |
| `backend/src/TcmClinic.Api.Core/UserAggregate/EmailAddress.cs` | 新增：Vogen VO |
| `backend/src/TcmClinic.Api.Core/UserAggregate/PasswordPolicy.cs` | 新增：12～128＋大寫＋小寫＋數字（全系統唯一規則來源） |
| `backend/src/TcmClinic.Api.Core/UserAggregate/PasswordExpiry.cs` | 新增：剩餘天數純函式 |
| `backend/src/TcmClinic.Api.Core/UserAggregate/Specifications/UserSpecs.cs` | 修改：+依帳號取可追蹤使用者（`UserByAccountForUpdateSpec`） |
| `backend/src/TcmClinic.Api.Core/Auditing/UserLog.cs` | 新增：append-only 實體（`[NotAudited]`、`IAuditable`、`ISoftDelete`） |
| `backend/src/TcmClinic.Api.Core/Auditing/UserLogStatus.cs` | 新增：SmartEnum Success／Failure |
| `backend/src/TcmClinic.Api.Core/Auditing/IUserLogRecorder.cs` | 新增：記錄服務抽象 |
| `backend/src/TcmClinic.Api.UseCases/Profile/GetMyProfile.cs` | 新增：查詢本人（含角色、剩餘天數、依 `BranchId` 取分行） |
| `backend/src/TcmClinic.Api.UseCases/Profile/UpdateMyProfile.cs` | 新增：修改本人（目前密碼驗證、鎖定、原子寫入、操作紀錄、非預期錯誤回 `CriticalError`） |
| `backend/src/TcmClinic.Api.UseCases/Profile/MyProfileDto.cs` | 新增 |
| `backend/src/TcmClinic.Api.UseCases/Profile/PasswordPolicyOptions.cs` | 新增：`ValidDays`（預設 90，`[Range(1, 3650)]`） |
| `backend/src/TcmClinic.Api.UseCases/Users/CreateUser.cs`、`ResetPassword.cs` | 修改：`SetPassword` 帶時間（`TimeProvider`） |
| `backend/src/TcmClinic.Api.UseCases/Auth/GetUserRolesByAccount.cs` | 修改：結果加帶 `DisplayName`（R12） |
| `backend/src/TcmClinic.Api.Infrastructure/Data/Config/UserConfig.cs` | 修改：三個新欄位映射與說明；`BranchId` 外鍵（Restrict，R15） |
| `backend/src/TcmClinic.Api.Infrastructure/Data/Config/UserLogConfig.cs` | 新增：`SysUserLog` 綱要、索引、說明 |
| `backend/src/TcmClinic.Api.Infrastructure/Data/AppDbContext.cs` | 修改：`DbSet<UserLog>` |
| `backend/src/TcmClinic.Api.Infrastructure/Data/Auditing/UserLogRecorder.cs` | 新增：獨立 scope／DbContext，盡力寫入 |
| `backend/src/TcmClinic.Api.Infrastructure/Data/Migrations/20260929031228_AddPersonalDataAndUserLog.cs` | 新增：`SysUser` 三欄、`SysUserLog` |
| `backend/src/TcmClinic.Api.Infrastructure/Data/Migrations/20260930074150_SysUserLogStandardColumns.cs` | 新增：`SysUserLog` 改五個標準欄位（R8 修訂） |
| `backend/src/TcmClinic.Api.Infrastructure/Data/Migrations/20260930075153_UserBranchIdFk.cs` | 新增：刪 `BankId`、加 `BranchId` 外鍵（R15） |
| `backend/src/TcmClinic.Api.Infrastructure/Data/LcApplicationSeedData.cs`、`BranchSeedData.cs` | 修改：admin 以 `AssignBranch` 指派示範分行（R15） |
| `backend/src/TcmClinic.Api.Infrastructure/Data/SeedData.cs` | 修改：`SetPassword` 帶時間 |
| `backend/src/TcmClinic.Api.Infrastructure/Data/ChbMenuSeedData.cs` | 修改：`personalData` 葉節點與 `SharedLeafGrants`（五角色） |
| `backend/src/TcmClinic.Api.Infrastructure/InfrastructureServiceExtensions.cs` | 修改：註冊 `IUserLogRecorder` |
| `backend/src/TcmClinic.Api.Web/Endpoints/Profile/GetMyProfileEndpoint.cs` | 新增：`GET /profile` |
| `backend/src/TcmClinic.Api.Web/Endpoints/Profile/UpdateMyProfileEndpoint.cs` | 新增：`PUT /profile`（含 Validator） |
| `backend/src/TcmClinic.Api.Web/Extensions/PasswordPolicyValidatorExtensions.cs` | 新增：`MustSatisfyPasswordPolicy()` |
| `backend/src/TcmClinic.Api.Web/Extensions/ResultExtensions.cs` | 修改：`ResultStatus.CriticalError` → 500（附 detail） |
| `backend/src/TcmClinic.Api.Web/Endpoints/Users/CreateUserEndpoint.cs`、`ResetPasswordEndpoint.cs` | 修改：改用共用密碼規則、更新說明文字 |
| `backend/src/TcmClinic.Api.Web/Configurations/AuthConfigs.cs` | 修改：每請求刷新 `GivenName`（R12） |
| `backend/src/TcmClinic.Api.Web/Services/HttpContextCurrentUser.cs`、`backend/src/TcmClinic.Api.Web/Program.cs` | 修改：`ICurrentUser` 註冊順序修正（R14）；綁定 `PasswordPolicyOptions` |
| `backend/src/TcmClinic.Api.Web/appsettings.json` | 修改：`PasswordPolicy:ValidDays = 90` |
| `docs/SECURE-CODING.md` | 修改：§6 補「密碼強度規則以 Core `PasswordPolicy` 為唯一來源」 |

已不存在的歷史變更點：`backend/src/TcmClinic.Api.UseCases/LcApplications/CurrentUserBank.cs`（R15 時改為依 `BranchId` 組回機構代碼，其後由 SPEC-011 收斂移除）、`backend/src/TcmClinic.Api.UseCases/Branches/BranchNameLookup.cs`（R15 刪除）。

### 資料模型

本功能擴充既有 `User` 聚合（表 `SysUser`），新增兩個值物件、兩個 Core 純函式規則、一個 append-only 操作紀錄表 `SysUserLog`。遷移名稱：`AddPersonalDataAndUserLog`（其後修訂：`SysUserLogStandardColumns`、`UserBranchIdFk`）。

#### 原 data-model.md

> 以下 § 編號沿用原文件，供程式碼註解（如「data-model §1.1、§4」）追溯。

##### §1 User 聚合（既有，擴充）— 表 `SysUser`

###### 1.1 欄位異動

| 屬性（C#） | 欄位 | 型別 | Null | 說明（`HasComment`，zh-TW） | 異動 |
|-----------|------|------|------|---------------------------|------|
| `Phone` | `Phone` | `nvarchar(20)` | ✅ | 聯絡電話（選填；數字、空白與 + - ( ) #） | **新增** |
| `Email` | `Email` | `nvarchar(100)` | ✅ | 電子郵件（選填） | **新增** |
| `PasswordChangedAt` | `PasswordChangedAt` | `datetime` | ✅ | 上次修改密碼時間（本地時間；NULL 表示上線前建立、以 CreateDate 代替） | **新增** |
| `Account` | `Account` | `nvarchar(50)` | ❌ | 登入帳號 | 不變（本功能唯讀） |
| `DisplayName` | `DisplayName` | `nvarchar(100)` | ❌ | 使用者顯示名稱 | 不變（本功能可改） |
| `BranchId` | `BranchId` | `int`（`BranchId` Vogen） | ✅ | 所屬分行（FK → `Branch.Id`，`OnDelete: Restrict`；NULL 表示未歸屬） | **取代 `BankId`**（2026-09-30，DATABASE-SCHEMA §5、R15；本功能唯讀） |
| `PasswordHash`/`PasswordSalt` | — | — | ❌ | `[SensitiveData]` | 不變（本功能可改） |

- 三個新欄位皆可空，既有列不回填（R5、R6）。
- `BankId`（7 碼機構代碼字串）刪除、以 `BranchId` 取代；既有值不轉換，開發種子改以 `AssignBranch` 指派示範分行（R15）。`SetBankId` 改為 `AssignBranch(BranchId?)`。
- `Phone`、`Email` **不**標記 `[SensitiveData]`（R6）；`PasswordHash`/`PasswordSalt` 維持標記，異動紀錄照舊遮罩。

###### 1.2 領域方法

| 方法 | 行為 | 不變式 |
|------|------|--------|
| `SetPassword(string hash, string salt, DateTime changedAt)` | 覆寫雜湊與 salt，並設 `PasswordChangedAt = changedAt` | **簽章變更**：所有設定密碼的呼叫端（CreateUser、ResetPassword、SeedData、本人變更）皆須傳入時間 |
| `Rename(UserName)` | 既有 | — |
| `UpdateContact(PhoneNumber? phone, EmailAddress? email)` | 設定聯絡電話與電子郵件；`null` 表示清除 | 新增 |

###### 1.3 衍生值（不落庫，查詢時計算）

| 衍生值 | 計算 |
|--------|------|
| 有效的上次修改密碼時間 | `PasswordChangedAt ?? CreateDate` |
| 密碼尚餘有效天數 | `PasswordExpiry.RemainingDays(DateOnly(有效時間), DateOnly(今日), ValidDays)` |
| 是否已逾期 | 剩餘天數 `== 0` |
| 帳號狀態 | 目前固定 `Enabled`（R10） |
| 所屬分行代碼／名稱 | 依 `BranchId` 查分行主檔取 4 碼 `Code` 與中文名稱；`BranchId` 為 NULL 或查無時兩者皆為 `null`（R10 → SPEC-014 FR-019 → R15） |

##### §2 值物件（新增，Vogen）

###### 2.1 `PhoneNumber`（`Core/UserAggregate/PhoneNumber.cs`）

| 項目 | 規則 |
|------|------|
| 基底 | `[ValueObject<string>]` |
| NormalizeInput | `Trim()` |
| Validate | 長度 1～20；僅允許 `0-9`、空白、`+`、`-`、`(`、`)`、`#` |
| 錯誤訊息 | 「聯絡電話僅可包含數字、空白與 + - ( ) #，且不超過 20 字元。」 |

###### 2.2 `EmailAddress`（`Core/UserAggregate/EmailAddress.cs`）

| 項目 | 規則 |
|------|------|
| 基底 | `[ValueObject<string>]` |
| NormalizeInput | `Trim()`（不轉小寫，保留使用者輸入） |
| Validate | 長度 1～100；一般 email 格式：恰一個 `@`、local 與 domain 皆非空、domain 含 `.`、不含空白 |
| 錯誤訊息 | 「電子郵件格式不正確或超過 100 字元。」 |

> 「空字串／null ＝ 清除」由用例處理：輸入為 null 或空白時不建立 VO，直接傳 `null` 給 `UpdateContact`。

##### §3 領域規則（新增，Core 純函式）

###### 3.1 `PasswordPolicy`（`Core/UserAggregate/PasswordPolicy.cs`）

| 常數／方法 | 內容 |
|-----------|------|
| `MinLength` | 12 |
| `MaxLength` | 128 |
| `Validate(string password) → IReadOnlyList<string>` | 回傳所有未符合項訊息；空清單＝通過 |

規則與訊息：

| 規則 | 訊息 |
|------|------|
| 長度 < 12 或 > 128 | 「密碼長度須介於 12 至 128 碼。」 |
| 無 `[A-Z]` | 「密碼須包含英文大寫字母（A-Z）。」 |
| 無 `[a-z]` | 「密碼須包含英文小寫字母（a-z）。」 |
| 無 `[0-9]` | 「密碼須包含數字（0-9）。」 |

適用：新增使用者、管理者重設密碼、本人變更密碼（FR-008a）。既有密碼不追溯（登入不檢查規則）。

###### 3.2 `PasswordExpiry`（`Core/UserAggregate/PasswordExpiry.cs`）

`RemainingDays(DateOnly lastChanged, DateOnly today, int validDays) = clamp(validDays − (today.DayNumber − lastChanged.DayNumber), 0, validDays)`

| 範例（validDays=90） | 結果 |
|---------------------|------|
| 今日修改 | 90 |
| 31 天前修改 | 59（對齊舊系統畫面） |
| 90 天前修改 | 0（已逾期） |
| 修改日晚於今日（時鐘異常） | 90（上限為 `validDays`） |

不變式（CsCheck 屬性測試）：結果 ∈ [0, validDays]；`today` 每增加 1 天結果不增加；`lastChanged == today` 時結果 == `validDays`。

###### 3.3 設定：`PasswordPolicyOptions`（`UseCases/Profile/PasswordPolicyOptions.cs`）

| 屬性 | 預設 | 設定鍵 |
|------|------|--------|
| `ValidDays` | 90 | `PasswordPolicy:ValidDays` |

Web 啟動時以 `AddOptions<PasswordPolicyOptions>().Bind(...).ValidateDataAnnotations().ValidateOnStart()` 綁定並驗證（`ValidDays` 須介於 1～3650），再以 singleton 將其值注入用例；用例層不依賴 Options 框架（實作決議：UseCases 專案不新增 Microsoft.Extensions.Options 相依）。

##### §4 UserLog（新增）— 表 `SysUserLog`（append-only）

Core 實體 `Core/Auditing/UserLog.cs`，`[NotAudited]`，實作 `IAuditable`、`ISoftDelete`。依 DATABASE-SCHEMA §6：主鍵可採 `bigint IDENTITY`，但 §1 五個標準欄位不得省略（見「原則符合性」Complexity Tracking；2026-09-30 修訂，原 `OperatedBy`／`OperatedAt` 已廢除，R8）。

| 屬性 | 欄位型別 | Null | 說明（`HasComment`） |
|------|---------|------|---------------------|
| `Id` | `bigint IDENTITY` | ❌ | 主鍵：操作紀錄流水號（資料庫產生） |
| `FunctionId` | `nvarchar(50)` | ❌ | 功能代碼（本功能：`PersonalData`） |
| `Action` | `nvarchar(50)` | ❌ | 動作（本功能：`Modify`） |
| `Status` | `nvarchar(20)` | ❌ | 結果：`Success`／`Failure` |
| `Content` | `nvarchar(500)` | ❌ | 操作內容訊息（不得含密碼） |
| `ClientIp` | `nvarchar(45)` | ✅ | 來源用戶端 IP |
| `CreateDate`／`CreateUser` | `datetime`／`nvarchar(50)` | ❌ | 操作時間與操作者帳號（無登入者時 `system`／`anonymous`） |
| `ModifyDate`／`ModifyUser` | 同上 | ❌ | 與 Create* 同值（列不會被更新） |
| `Removed` | `bit`，預設 `0` | ❌ | 恆為 `0`（列不會被軟刪除） |

- 五個標準欄位由既有 `AuditableEntityInterceptor` 於新增時自動填入，欄位說明以 `AuditableColumnComments` 共用設定套用；照常套用軟刪除全域過濾器。
- 表說明：「使用者操作紀錄（append-only）：記錄重要業務操作的成功／失敗，供『使用者存取記錄』查詢」。
- 索引（EF 預設命名，DATABASE-SCHEMA §8）：`IX_SysUserLog_CreateUser_CreateDate (CreateUser, CreateDate)`、`IX_SysUserLog_FunctionId_CreateDate (FunctionId, CreateDate)`。
- `Status` 以 SmartEnum `UserLogStatus { Success, Failure }` 表達，持久化其 `Name`（比照 `AccessType`）。

###### 4.1 `IUserLogRecorder`（`Core/Auditing/IUserLogRecorder.cs`）

```text
Task RecordAsync(UserLogRequest request, CancellationToken ct)
UserLogRequest(string FunctionId, string Action, UserLogStatus Status, string Content)
```

- IP 由實作自 `ICurrentUser` 取得；操作者與時間由攔截器填入標準欄位（獨立 scope 內的 `ICurrentUser` 仍須解析為登入者，須有測試鎖住）。
- 實作（`Infrastructure/Data/Auditing/UserLogRecorder.cs`）以 `IServiceScopeFactory` 建立獨立 `AppDbContext` 寫入；**盡力寫入、失敗不拋**，失敗時 `LogError("[USER_LOG] …")`（R8）。

##### §5 選單種子（既有，擴充）— `ChbMenuSeedData`

| 項目 | 做法 |
|------|------|
| 選單節點 | `personalData` 葉節點只放在 SM 選單樹既有的 `管理業務(management) › 系統管理作業(system)` 容器下（`rolePermission` 之後） |
| 可見性 | 新增 `SharedLeafGrants`（跨角色共用葉節點 → 可見角色），於可見性種入階段把 `personalData` 授予 BH、BS、SM、MB、BM |

- **實作決議**：不把此路徑加進 BH 等其他角色的選單樹。種子依樹的先後建立節點，BH 最先處理；若在 BH 樹中新增 `management` 容器，會改變後續所有節點的建立順序與 Id，使既有選單的同層排序改變（`/me/menus` 契約快照因此出現非預期差異）。改由可見性階段直接授權，選單樹只多一個葉節點。
- 同層順序對齊 `useMenu.ts`：`rolePermission`(0) → `accountMaintenance`(1) → `personalData`(2)；`accountMaintenance` 尚未種入，`personalData` 仍以 2 為 `DisplayOrder`。
- 冪等：重跑不產生重複選單或可見性。

##### §6 驗證規則總表（對照 FR）

| 欄位 | 規則 | 驗證位置 | FR |
|------|------|---------|----|
| `displayName` | 必填（trim 後非空）、≤100 | Validator + `UserName` VO | FR-004 |
| `phone` | 選填；≤20；字元集 | Validator + `PhoneNumber` VO | FR-005 |
| `email` | 選填；≤100；格式 | Validator + `EmailAddress` VO | FR-006 |
| `newPassword` | null/"" ＝ 不變更；否則 `PasswordPolicy` | Validator + 用例防禦檢查 | FR-008 |
| `confirmNewPassword` | 有新密碼時必填且相等；只填確認而無新密碼亦拒絕 | Validator | FR-008 |
| `currentPassword` | 有新密碼時必填；須驗證正確；鎖定中拒絕 | Validator（必填）+ 用例（正確性、鎖定） | FR-009 |
| 新密碼 ≠ 目前密碼 | `Verify(new, hash, salt) == false` | 用例 | FR-008 |

##### §7 狀態與交易

- 無狀態機。
- 單次修改＝一個用例、一次 `SaveChanges`（R9）；操作紀錄於獨立 DbContext 寫入，不參與主交易。

### API 契約

風格沿用專案既有慣例（FastEndpoints REPR、`Result` → HTTP/ProblemDetails、JSON camelCase、訊息 zh-TW）。前端消費點：`CHB.Web/src/views/PersonalDataView.vue`（目前為空殼，由前端團隊依本契約實作；需新增「目前密碼」欄位，見 Assumptions）。

#### 原 contracts/profile.md

> 以下 § 編號沿用原契約文件，供程式碼註解（如「contracts/profile.md §1」）追溯。

兩個端點授權皆為 `MenuAccess:personalData`（伺服器端強制，FR-014），需配套種子提供 `personalData` 選單項並授予 CHB 五角色（資料模型 §5）。

**對象一律為登入者本人**：路由與請求皆不含使用者識別；即使用戶端夾帶 `id`、`account` 等欄位也不繫結、不影響結果（FR-002、R1）。

##### §1 查詢本人個人資料 `GET /profile`

- **用途**：US1、US4。
- **200 回應**：

  ```json
  {
    "account": "everyone",
    "displayName": "everyone",
    "branchCode": "0001",
    "branchName": "零零零壹分行",
    "roles": [
      { "code": "BH", "name": "經辦" }
    ],
    "phone": null,
    "email": null,
    "passwordChangedAt": "2026-08-27",
    "passwordRemainingDays": 59,
    "passwordExpired": false,
    "status": "Enabled"
  }
  ```

| 欄位 | 型別 | 說明 |
|------|------|------|
| `account` | string | 帳號（唯讀） |
| `displayName` | string | 使用者名稱 |
| `branchCode` | string \| null | 所屬分行代碼（4 碼）；未歸屬分行時為 `null`（R15：2026-09-30 前為 7 碼機構代碼、未歸屬為 `""`） |
| `branchName` | string \| null | 所屬分行中文名稱；未歸屬分行時為 `null`（分行主檔由 SPEC-014「分行資料維護」提供，其 FR-019） |
| `roles` | `{ code, name }[]` | 所屬角色；依角色排序；無角色時 `[]` |
| `phone` | string \| null | 聯絡電話 |
| `email` | string \| null | 電子郵件 |
| `passwordChangedAt` | string（`yyyy-MM-dd`） | 上次修改密碼日期；無紀錄時為帳號建立日 |
| `passwordRemainingDays` | integer（0～效期天數） | 密碼尚餘有效天數 |
| `passwordExpired` | boolean | 是否已逾期（僅顯示，不限制使用，FR-012） |
| `status` | `"Enabled"` \| `"Disabled"` | 帳號狀態；停用機制上線前恆為 `"Enabled"` |

- **不含**任何密碼、雜湊、salt（FR-001、SC-006）。
- **401**：未登入。**403**：無 `personalData` 選單可見性。
- **404**：登入者在資料庫中已不存在。正常情況到不了這裡：Cookie 每請求重驗會先把已刪除的使用者擋為 401；僅作為用例層的防禦性回應。

##### §2 修改本人個人資料（含選擇性變更密碼）`PUT /profile`

- **用途**：US2、US3。
- **請求**：

  ```json
  {
    "displayName": "everyone",
    "phone": "02-2345-6789#123",
    "email": "everyone@example.com",
    "currentPassword": "OldPassw0rd12",
    "newPassword": "NewPassw0rd34",
    "confirmNewPassword": "NewPassw0rd34"
  }
  ```

| 欄位 | 必填 | 規則 |
|------|------|------|
| `displayName` | ✅ | trim 後非空、≤100（FR-004） |
| `phone` | — | `null`／`""`＝清除；≤20，僅 `0-9`、空白、`+ - ( ) #`（FR-005） |
| `email` | — | `null`／`""`＝清除；≤100，email 格式（FR-006） |
| `newPassword` | — | `null`／`""`＝不變更密碼；否則須符合密碼規則 12～128 碼且含大寫、小寫、數字（FR-008）。**僅含空白視為有填寫** |
| `confirmNewPassword` | 條件 | 有 `newPassword` 時必填且須相等；無 `newPassword` 卻有值 → 拒絕 |
| `currentPassword` | 條件 | 有 `newPassword` 時必填且須正確（FR-009）；無 `newPassword` 時忽略 |

- **200 回應**：

  ```json
  { "message": "個人資料維護 (everyone) [everyone] 修改成功" }
  ```

  前端收到後以 `GET /profile` 重新載入（剩餘天數、上次修改密碼時間已更新）。

- **400 ValidationProblem**（整筆不寫入，FR-007）：欄位層級錯誤，`errors` 鍵為欄位名（camelCase）。

| 情境 | 欄位 | 訊息 |
|------|------|------|
| 使用者名稱空白 | `displayName` | 使用者名稱不可為空白。 |
| 使用者名稱過長 | `displayName` | 使用者名稱長度須介於 1 至 100 字元。 |
| 電話格式／長度不符 | `phone` | 聯絡電話僅可包含數字、空白與 + - ( ) #，且不超過 20 字元。 |
| email 格式／長度不符 | `email` | 電子郵件格式不正確或超過 100 字元。 |
| 新密碼不符規則 | `newPassword` | 逐項列出（見資料模型 §3.1），可多筆 |
| 只填新密碼 | `confirmNewPassword` | 新密碼確認不可為空白。 |
| 只填新密碼確認 | `newPassword` | 新密碼不可為空白。 |
| 兩次不一致 | `confirmNewPassword` | 新密碼與新密碼確認內容不一致。 |
| 未填目前密碼 | `currentPassword` | 目前密碼不可為空白。 |
| 目前密碼錯誤 | `currentPassword` | 目前密碼不正確。 |
| 錯誤次數達鎖定門檻 | `currentPassword` | 嘗試次數過多，請稍後再試。 |
| 新密碼與目前密碼相同 | `newPassword` | 密碼不可與上次相同。 |

- **401**：未登入。**403**：無 `personalData` 選單可見性。
- **404**：登入者已不存在（正常情況由每請求重驗先回 401；收斂任務 T054 補入端點說明）。
- **500 ProblemDetails**：非預期錯誤，`detail` 為「個人資料維護 (帳號) [名稱] 修改失敗」（不留部分寫入，並寫入失敗操作紀錄）。

**副作用**

| 條件 | 副作用 |
|------|--------|
| 成功 | 更新名稱／電話／email；有新密碼時覆寫雜湊、`PasswordChangedAt = 現在`、重置登入失敗計數；`SysAuditLog` 由既有攔截器記錄（密碼欄位遮罩）；`SysUserLog` 記 `Success` |
| 目前密碼錯誤 | 登入失敗計數 +1（與登入共用，達 5 次鎖定約 15 分鐘）；`SysUserLog` 記 `Failure` |
| 鎖定中、與目前密碼相同、規則防禦檢查失敗、非預期例外 | `SysUserLog` 記 `Failure` |
| 格式驗證失敗（Validator 400） | 無（未進入用例，R8） |

##### §3 既有端點的連帶變更

| 端點 | 變更 | 依據 |
|------|------|------|
| `POST /users` | 密碼規則改為 12～128 碼且含大寫、小寫、數字；400 訊息改為逐項列出 | FR-008a |
| `PUT /users/{id}/password` | 同上；重設成功時更新 `PasswordChangedAt` | FR-008a、FR-011 |
| `GET /auth/me` | `displayName` 每請求自資料庫刷新（改名後立即反映） | R12 |

回應形狀皆不變。

### 設計決策（原 research）

> 決策編號沿用原設計研究文件（R1–R15），供程式碼註解「research R*」「plan R*」追溯。

#### R1 端點路由與「本人」判定（FR-002，防 IDOR）
- **決定**：新增 `GET /profile`、`PUT /profile` 兩個端點，路由**不含任何使用者識別**；用例一律以 `ICurrentUser.UserName`（已驗證 Cookie 的帳號 claim）查出登入者本人。請求 DTO 不提供 `id`／`account` 欄位，即使用戶端夾帶亦不會被繫結。授權：`Policies("MenuAccess:personalData")`。
- **理由**：從路由與 DTO 形狀上根本消除「以參數指定修改對象」的可能（修正舊系統 `userId` 隱藏欄位漏洞），物件層級授權天然成立（SECURE-CODING §4）。授權字串比照 SPEC-012 以前端 canonical 選單 value 為準。
- **替代方案**：
  - `PUT /users/{id}` 加「id 必須等於本人」檢查：可行但易被日後改動破壞，且與管理者用的 `/users/{id}`（當時為 `MenuAccess:/admin/users`）授權語意混淆。
  - `/users/me/profile`：與既有 `/users/{id}` 路由樣式衝突（`me` 需另設路由約束），不採。
  - `/personal-data`：與選單同名但非資源名詞；`/profile` 較符合 REST 語意，選單對應以授權字串表達即可。

#### R2 密碼強度規則集中化（FR-008、FR-008a、SC-003a）
- **決定**：於 Core 新增純函式 `PasswordPolicy`（`UserAggregate/PasswordPolicy.cs`）：長度 12～128，且須同時含 `[A-Z]`、`[a-z]`、`[0-9]`；`Validate(string) → IReadOnlyList<string>`（回傳所有未符合項的 zh-TW 訊息）。Web 層提供單一 FluentValidation 擴充 `MustSatisfyPasswordPolicy()`，由 `CreateUserValidator`、`ResetPasswordValidator`、`UpdateMyProfileValidator` 共用；用例層（變更本人密碼）再防禦性呼叫一次（分層驗證）。移除兩處重複的 `MinimumLength(8)…Matches(...)`。
- **理由**：三個入口必須對同一密碼得到一致結果（SC-003a），唯一可靠做法是單一規則來源；放 Core 使其零框架相依、可做 CsCheck 屬性測試（TESTING §4.1 值規則）。
- **替代方案**：
  - 各 Validator 各自寫規則：即現況，已證明會漂移（8 碼規則寫了兩份）。
  - 做成 Vogen `Password` 值物件：明文密碼不應以領域型別存活於聚合內（明文不落地），且其生命週期僅在單一請求，純函式即足。
- **既有影響**：既有功能測試建立使用者用的 `P@ssw0rd1`（9 碼，約 19 處）須改為符合新規則的 12 碼以上密碼；`UserCrudEndpointsTests` 的弱密碼案例改為 12 碼邊界。種子管理員密碼 `Admin123!` 屬既有密碼、不追溯，仍可登入。

#### R3 「未提供新密碼」的判定
- **決定**：`newPassword` 為 `null` 或空字串（`""`）才視為「不變更密碼」；**僅含空白字元視為有填寫**，照常進入規則驗證（必失敗於長度或字元組成）。驗證器不可使用 FluentValidation `NotEmpty()`（它把空白視為空），改以 `string.IsNullOrEmpty` 判斷是否啟動密碼規則。
- **理由**：對應 Edge Case「新密碼欄位僅含空白字元：視為有填寫」，避免使用者以為已改密碼。
- **替代方案**：Trim 後判斷：會把「誤按空白」靜默當成未變更，違反 SECURE-CODING §3「拒絕而非默默修正」。

#### R4 變更密碼須驗證目前密碼與防暴力猜測（FR-009、Assumptions）
- **決定**：
  1. 有填新密碼時 `currentPassword` 必填；以 `IPasswordHasher.Verify(current, hash, salt)` 驗證。
  2. **目前密碼錯誤併入既有登入鎖定計數**（`ILoginThrottle`，鍵＝正規化帳號）：錯誤時 `RegisterFailure`；變更前若 `IsLocked` 為真則直接拒絕（欄位 `currentPassword`：「嘗試次數過多，請稍後再試」）；驗證正確並完成變更後 `Reset`。
  3. 「新密碼不可與目前密碼相同」以 `Verify(newPassword, 目前 hash, salt)` 判定。
- **理由**：已登入工作階段若被盜用，攻擊者可透過本端點無限猜測目前密碼；共用登入鎖定可在 5 次失敗後同時封住「此端點猜測」與「以猜中密碼重新登入」。沿用既有機制，不引入新元件。
- **替代方案**：
  - 不限制：違反憲章原則 I。
  - 本端點另設獨立計數：兩套計數可被交替利用（各猜 4 次），且多一個元件。
  - 回 429：語意較精確，但前端需另處理；本功能統一以 400 欄位錯誤回報，前端顯示邏輯一致（Principle V 以既有錯誤形狀為準）。

#### R5 「上次修改密碼時間」與密碼效期（FR-010、FR-011、SC-005）
- **決定**：
  - `SysUser` 新增可空欄位 `PasswordChangedAt`（`datetime`，本地時間，與稽核欄位一致）。**不回填**既有資料；計算時 `PasswordChangedAt ?? CreateDate`。
  - `User.SetPassword(hash, salt, changedAt)` 簽章加上變更時間，**所有**設定密碼的路徑（新增使用者、管理者重設、本人變更、種子）都經此更新時間。時間由用例注入的 `TimeProvider`（已註冊 `TimeProvider.System`）取得，便於測試固定時間。
  - Core 新增純函式 `PasswordExpiry.RemainingDays(DateOnly lastChanged, DateOnly today, int validDays) = clamp(validDays − (today − lastChanged).Days, 0, validDays)`（上限防時鐘異常）；`IsExpired = RemainingDays == 0`。
  - 效期天數由設定 `PasswordPolicy:ValidDays`（預設 90）以 Options 取得；日後「系統參數維護」上線時改由其提供。
- **理由**：公式對齊舊系統 `RemnantExpireDate`（修改當日＝90、31 天前＝59，US4）；舊系統逾期後仍顯示 1，新規格改為 0 並明確標示逾期（FR-011）。可空欄位＋查詢時回退，比 migration 回填更誠實（不捏造修改時間）。
- **替代方案**：
  - Migration 以 `CreateDate` 回填：結果相同但把推測值寫成事實。
  - 設定值寫死 90：違反「保留可調整的設定來源」（Assumptions）。
  - `DateTime.Now` 直接呼叫：難以測試日期邊界。
- **後續**：SPEC-016 系統參數維護決定密碼效期不納入系統參數，維持部署設定 `PasswordPolicy:ValidDays`。

#### R6 聯絡電話與電子郵件的型別與敏感度（FR-005、FR-006、Principle IV）
- **決定**：新增 Vogen 值物件 `PhoneNumber`（≤20，允許 `0-9`、空白、`+ - ( ) #`）與 `EmailAddress`（≤100，一般 email 格式，trim）。`User` 以可空屬性 `PhoneNumber? Phone`、`EmailAddress? Email` 持有；空字串或 null 輸入＝清除（存 `NULL`）。**不標記** `[SensitiveData]`。
- **理由**：以強型別表達業務概念（Principle IV）。本專案 `[SensitiveData]` 的既有語意是「憑證」（`PasswordHash`/`PasswordSalt`），標記後值會在異動紀錄被遮罩；聯絡資訊的異動軌跡對稽核有價值，比照 `DisplayName` 不標記。日誌面：`LoggingBehavior` 只記訊息型別名、請求記錄不含 body（`LoggingRedactionTests` 護欄），聯絡資訊與密碼不會進 Serilog。
- **替代方案**：
  - 裸 `string`：違反 Principle IV。
  - 標記 `[SensitiveData]`：異動紀錄全成 `***`，失去「誰改了誰的聯絡方式」的追溯價值；且會使 `SysAccessLog` 判定範圍擴大。

#### R7 使用者名稱長度（FR-004 調整）
- **決定**：使用者名稱上限採系統既有 `UserName` 值物件規則 **1～100 字元**，不採舊系統的 30。已同步修訂 FR-004 並註明原因。
- **理由**：管理者在「使用者管理」可設定到 100 字元；若本頁限 30，名稱較長的使用者連改電話都無法儲存（名稱為必填欄位）。同一概念同一規則（Principle V）。
- **替代方案**：維持 30：與既有 VO、資料欄位（nvarchar(100)）矛盾，須另寫例外規則。

#### R8 操作紀錄（FR-015、SC-007）
- **決定**：新增 append-only 系統表 **`SysUserLog`**（Core 實體 `UserLog`，`[NotAudited]`，`bigint IDENTITY`），欄位對齊舊系統 `USER_LOG`：功能代碼、動作、結果（Success/Failure）、內容訊息、操作者帳號、來源 IP、時間。Core 定義 `IUserLogRecorder`；Infrastructure 實作以 **`IServiceScopeFactory` 建立獨立 `AppDbContext`** 寫入，盡力寫入、失敗不拋（比照 `AccessLogRecorder` 的 `[USER_LOG]` 告警）。
  - 記錄範圍：請求通過格式驗證（FluentValidation）後進入用例的每一次修改——成功、業務性失敗（目前密碼錯誤、鎖定中、與目前密碼相同、密碼規則防禦檢查失敗）、非預期例外皆記一筆。格式驗證 400（請求本身不成立、未進入用例）不記；FR-015 已同步註明此定義。
  - 內容格式：`個人資料維護 ({帳號}) [{使用者名稱}] 修改成功`；失敗附原因代碼（如 `修改失敗：目前密碼不正確`）。**任何密碼內容不得入內容**。
  - 查詢畫面由「使用者存取記錄」規格負責；本表即其資料來源。
- **理由**：`SysAuditLog`（SPEC-006）只在資料成功寫入時產生，無法記「失敗」；`SysAccessLog`（SPEC-010）語意是敏感資料讀取，混用會污染其查詢。獨立 DbContext 保證主交易失敗（例外、ChangeTracker 殘留）時失敗紀錄仍可落庫，且不被主交易回滾。
- **替代方案**：
  - 只寫 Serilog：不可由系統內查詢，「使用者存取記錄」無資料源。
  - 擴充 `SysAccessLog` 加 `Modify` 類型：違反其「讀取旁路」定義與既有索引設計。
  - 共用請求範圍的 DbContext：主 SaveChanges 失敗後 ChangeTracker 仍含髒資料，再 SaveChanges 會把失敗的修改一起送出。
- **Complexity**：使用 DATABASE-SCHEMA §6 append-only 例外，已記於「原則符合性」Complexity Tracking。
- **2026-09-30 修訂**：DATABASE-SCHEMA §1、§6 改為所有表（含 append-only 表）皆須具備五個標準欄位，`OperatedBy`／`OperatedAt` 改為 `CreateUser`／`CreateDate`（`ModifyDate`／`ModifyUser` 與其同值、`Removed` 恆為 0），由既有攔截器填入；索引改用 EF 預設命名。僅保留 `bigint IDENTITY` 主鍵一項例外。

#### R9 修改的原子性（FR-007、SC-003）
- **決定**：單一命令 `UpdateMyProfileCommand` 承載全部欄位；用例先完成所有檢查（VO 建構、目前密碼、規則、相同密碼），**全部通過後**才變更聚合並呼叫一次 `UpdateAsync`（單一 SaveChanges）。任一檢查失敗即回 `Result.Invalid`，不觸及聚合。
- **理由**：避免「基本資料已改、密碼沒改」的部分成功（Edge Case）。
- **替代方案**：拆成「改基本資料」「改密碼」兩個端點：與舊畫面一次送出的流程不符，且前端需處理半成功。

#### R10 所屬分行名稱與帳號狀態（FR-001、FR-013）
- **決定**：
  - 分行代碼取 `User.BankId`；分行名稱目前**回傳 `null`**（尚無分行主檔，「分行資料維護」未實作）。契約欄位先定義，待分行主檔上線時由查詢補值，不改契約。
  - 帳號狀態：`SysUser` 目前無啟用/停用欄位；能通過每請求重驗（未刪除且有角色）者即回 `"Enabled"`。契約定義 `"Enabled" | "Disabled"` 兩值，停用機制由「帳號維護」引入。
- **理由**：契約先穩定、資料來源後補，前端不需二次改版；不在本功能預先建立分行主檔或狀態欄位（避免越界到他人規格）。
- **替代方案**：本功能先建 `Branch` 表：屬「分行資料維護」範圍，會造成兩份規格同時定義同一實體。
- **後續**：分行名稱已由 SPEC-014「分行資料維護」接手，改由分行主檔提供（其 FR-019）；2026-09-30 起使用者所屬分行改為 `BranchId` 外鍵（R15）。

#### R11 選單種子（FR-014）
- **決定**：`personalData` 葉節點放在 SM 選單樹既有的 `管理業務(management) › 系統管理作業(system)` 容器下；另以 `SharedLeafGrants` 於可見性種入階段授予 BH、BS、SM、MB、BM 五角色（實作決議：不改其他角色的選單樹，以免改變既有節點的建立順序與同層排序，見資料模型 §5）。`personalData` 的同層順序對齊 `useMenu.ts`（角色權限維護 → 帳號維護 → **個人資料維護**）。
- **理由**：個人資料維護是每位使用者都需要的功能；舊種子中五角色原本都有「個人設定（managerPersonal）」即為佐證。沿用 SPEC-012 補 `rolePermission` 的相同手法，冪等可重跑。
- **替代方案**：只給 BH（照 useMenu 目前只填 BH 選單）：其他角色登入後無法維護自己的密碼與聯絡資料，不合理。

#### R12 顯示名稱 claim 刷新
- **決定**：`GetUserRolesByAccountQuery` 的結果加帶 `DisplayName`，`AuthConfigs.OnValidatePrincipal` 每請求一併刷新 `ClaimTypes.GivenName`。
- **理由**：使用者改名後，`GET /auth/me`（前端頁首「登入者」）若仍顯示舊名稱會造成困惑；既有每請求重驗已查出使用者，順手刷新成本為零。
- **替代方案**：改名後要求重新登入：體驗差；在 `PUT /profile` 內重簽 Cookie：與既有「每請求刷新」機制重複。

#### R13 其他小決策

| 項目 | 決策 | 理由 |
|------|------|------|
| 並行控制 | 不使用 rowVersion，後寫入者生效 | 僅本人修改（Assumptions）；避免前端多帶權杖 |
| 變更密碼後工作階段 | 目前 Cookie 維持有效，不強制登出其他裝置 | Assumptions；既有無工作階段清單機制 |
| 查詢是否寫 `SysAccessLog` | `GET /profile` **不**記錄 | 回應不含任何 `[SensitiveData]` 欄位值；SPEC-010 的 Read 記錄針對敏感值外洩 |
| 回應訊息 | `PUT` 成功回 `200 { message }`，前端再 `GET` 刷新 | 對齊舊系統結果訊息頁（FR-016）；避免在寫入回應重複整份資料 |
| 預覽／列印 | 後端不提供 | 前端以 `GET /profile` 資料組成（Assumptions） |

#### R14 實作期間發現：`ICurrentUser` 註冊順序缺陷（既有問題，本功能修正）
- **發現**：`Program.cs` 先註冊 `AddScoped<ICurrentUser, HttpContextCurrentUser>()`，之後 `AddInfrastructureServices` 又註冊 `AddSingleton<ICurrentUser, SystemCurrentUser>()`。DI 取「最後註冊者」，導致 Web 執行時 `ICurrentUser.UserName` 恆為 `"system"`。
- **影響**：本功能以登入身分定位本人（FR-002），此缺陷使 `/profile` 一律找不到本人（404）。既有功能亦受影響：稽核欄位 `CreateUser`/`ModifyUser` 與 `SysAuditLog.OperatedBy` 皆記為 `system`、`SysAccessLog.AccessedBy` 非真實帳號、SPEC-011 受理作業以 `CurrentUserBank` 解析所屬機構時取不到使用者。
- **決定**：將 `HttpContextCurrentUser` 的註冊移到 `AddInfrastructureServices` 之後（符合 Infrastructure 註解「Web 層可覆寫」的原意），並附註解說明順序要求。
- **理由**：最小改動、恢復原設計意圖；既有測試皆不依賴 `"system"` 值（整合測試自行建構 `SystemCurrentUser`）。
- **後續**：自此稽核與訪問日誌將記錄真實登入帳號。為維持種子列原本的操作者標示，`HttpContextCurrentUser` 於無 HttpContext（啟動種子、背景作業）時回 `"system"`（與 `SystemCurrentUser` 一致），請求存在但未驗證時仍回 `"anonymous"`。

#### R15 所屬分行改為 ID 外鍵（2026-09-30，DATABASE-SCHEMA §5；Clarifications Session 2026-09-30）
- **決定**：
  - `SysUser.BankId`（7 碼機構代碼字串）改為 `BranchId?`（`BranchId` Vogen ID＋外鍵 → `Branch.Id`，`OnDelete: Restrict`，NULL＝未歸屬）；`User.SetBankId` 改為 `AssignBranch(BranchId?)`。
  - `GET /profile` 依 `BranchId` 以 `GetByIdAsync` 取分行（第 3 次查詢；不以導覽屬性 Include 其他聚合），回傳 4 碼 `branchCode` 與中文名稱；未歸屬或查無時兩者為 `null`。不再需要 `BranchOptions`。
  - SPEC-014 的 `BranchNameLookup`（依機構代碼批次查名稱）唯一呼叫端為本功能，改後刪除；SPEC-014 FR-019(a) 同步改為「依分行識別取得分行資料」。
  - SPEC-011 `CurrentUserBank` 改為依 `BranchId` 取分行後以 `InstitutionCode.Compose(Branch:BankCode, branch.Code)` 組回機構代碼；未歸屬時回 `""`，行為不變。SPEC-011 的 `ApplicantBankId` 等欄位與比對邏輯不在本次（留待 SPEC-011 處理；其後已由 SPEC-011 改為 ID 外鍵並移除 `CurrentUserBank`）。
  - 既有 `BankId` 值不轉換（尚無正式環境資料，唯一有值者為開發種子）；`LcApplicationSeedData` 改為在 `BranchSeedData` 之後將 admin 指派至示範分行 0001。
- **理由**：業務代碼可能變更、重用或跨來源不一致，不得當關聯鍵；外鍵由資料庫保證參照完整性（刪除仍被參照的分行會被擋下）。
- **替代方案**：
  - 保留 `BankId` 並另加 `BranchId`：雙寫，兩者必然漂移。
  - 連同 SPEC-011 一起改：超出本規格範圍（使用者裁決各規格逐一處理）。
  - Migration 以 SQL 解析回填：須同時寫 SQL Server／SQLite 語法並硬編碼銀行代碼 `009`，在無正式資料時成本高於價值。
- **契約影響**：`branchCode` 由 7 碼機構代碼（未歸屬 `""`）改為 4 碼分行代碼（未歸屬 `null`）；前端頁面仍為空殼，無既有消費者。

### 原則符合性

對照 [docs/CONSTITUTION.md](../../../../docs/CONSTITUTION.md)：初判與設計後複查皆 PASS。I 安全性——本人判定由伺服器依登入身分決定、路由無 id（防 IDOR）；變更密碼須驗證目前密碼且錯誤計入登入鎖定；密碼規則提升且全系統一致；密碼不入日誌／操作紀錄／異動紀錄（`[SensitiveData]` 遮罩）；伺服器端完整驗證。II 測試優先——每個 FR 先寫失敗測試（規則類 CsCheck 屬性測試、端點 Functional 契約測試、綱要／種子 Integration 測試）；既有測試密碼改 12 碼屬測試資料調整，不降低覆蓋。III 架構一致性——Core（VO、`PasswordPolicy`、`PasswordExpiry`、`UserLog`、`IUserLogRecorder`，零框架相依）／UseCases（Profile 查詢與命令）／Infrastructure（Config、Migration、Recorder、種子）／Web（端點、Validator 擴充），依賴方向不變。IV 類型安全——電話／email 以 Vogen VO、操作結果以 SmartEnum `UserLogStatus`。V 系統一致性——錯誤沿用 `Result.Invalid` → `ValidationProblem`；授權沿用 `MenuAccess:{menu value}`；使用者名稱長度採既有 100 字元規則。VI 效能——單筆操作、查詢次數固定、無 N+1。VII 繁體中文——規格、契約、欄位說明、錯誤訊息、註解皆 zh-TW。R4 共用鎖定、R8 獨立 DbContext 皆強化原則 I。

**Complexity Tracking**：

| 偏離 | 必要原因 | 為何不採較簡單方案 |
|------|----------|-------------------|
| `SysUserLog` 主鍵採 `bigint IDENTITY`（裸 `long`，非 Vogen ID） | append-only、永久保留、高寫入的日誌表；DATABASE-SCHEMA §6 允許此例外並要求於此記錄。五個標準欄位照常保留（2026-09-30 修訂，R8） | Vogen ID＋客戶端 `Max(Id)+1`：每次插入 O(n)，對高寫入日誌不可行 |
| `UserLogRecorder` 使用獨立 DbContext（非請求範圍共用） | 主交易失敗時仍須寫入「失敗」紀錄，且不可把主交易的髒資料一併送出（R8） | 共用 DbContext：失敗後 ChangeTracker 殘留未儲存的修改，再次 SaveChanges 會誤寫入；手動 Detach 易漏且脆弱 |

## Test Plan（測試計畫）

### 自動化測試

功能測試以「登入者本人」為對象，一律使用 `AuthWebApplicationFactory`（真實 Cookie 登入）：於測試內建立指派 CHB `BH` 角色、密碼符合新規則（如 `Passw0rdTest12`）的使用者後登入。登入鎖定相關案例使用各自專屬的測試帳號（計數存於行程記憶體、同一工廠內共用）。

- [x] 單元測試 `backend/tests/TcmClinic.Api.UnitTests/UserAggregate/PhoneNumberTests.cs`、`EmailAddressTests.cs`、`PasswordPolicyTests.cs`、`PasswordExpiryTests.cs`、`UserPersonalDataTests.cs`：VO、密碼規則與效期（CsCheck 屬性測試）、`User` 新方法（AC-7、AC-12、AC-17～AC-19、AC-33）。
- [x] 單元測試 `backend/tests/TcmClinic.Api.UnitTests/Profile/UpdateMyProfileHandlerTests.cs`：非預期錯誤、命令字串不含密碼（AC-23、AC-27）。
- [x] 整合測試 `backend/tests/TcmClinic.Api.IntegrationTests/Data/UserPersonalDataSchemaTests.cs`、`Data/PersonalDataMenuSeedTests.cs`、`Auditing/UserLogRecorderTests.cs`：綱要與說明、選單種子、操作紀錄獨立交易（AC-24、AC-25、AC-33）。
- [x] 功能測試 `backend/tests/TcmClinic.Api.FunctionalTests/ProfileEndpointsTests.cs`、`ProfilePasswordEndpointsTests.cs`、`ProfileExpiryEndpointsTests.cs`、`CurrentUserResolutionTests.cs`、`Contracts/ProfileContractTests.cs`：US1–US4 契約、IDOR、原子性、鎖定、操作紀錄、R14（AC-1～AC-19、AC-21～AC-33）。
- [x] 既有測試調整 `backend/tests/TcmClinic.Api.FunctionalTests/UserCrudEndpointsTests.cs`（12 碼規則、修改時間）、`LoggingRedactionTests.cs`（`PUT /profile` 三個密碼欄位）、`MenuAuthorizationTests.cs`、`MenuClaimsRevalidationTests.cs`、`SessionRevalidationTests.cs`、`backend/tests/TcmClinic.Api.IntegrationTests/Data/UserConfigTests.cs`（測試密碼升級為 12 碼以上）。

執行（依專案慣例以 `dotnet run --project` 執行測試專案）：

```powershell
dotnet run --project backend/tests/TcmClinic.Api.UnitTests
dotnet run --project backend/tests/TcmClinic.Api.IntegrationTests
dotnet run --project backend/tests/TcmClinic.Api.FunctionalTests
```

預期：全綠；既有使用者管理測試改用 12 碼密碼後不退化。

### 手動走查

前置：開發資料庫已套用最新 Migration；已種入 CHB 五角色與選單（含 `personalData`）；準備一個具 BH 角色、密碼為 12 碼以上且含大寫、小寫、數字（例 `Passw0rdTest12`）的測試帳號。

1. **US1 檢視**：以測試帳號 `POST /auth/login` 後呼叫 `GET /profile` → 200；`account` 為登入帳號、`roles` 含角色代碼與名稱；已指派分行者 `branchCode` 為 4 碼、`branchName` 為中文名稱，未歸屬者兩者皆 `null`；`status` 為 `Enabled`；回應中無 `password`、`hash`、`salt` 字樣。未登入 → 401；無 `personalData` 可見性的角色 → 403。
2. **US2 修改基本資料**：`PUT /profile` 只帶 `displayName`、`phone`、`email` → 200，`message` 為「個人資料維護 (帳號) [新名稱] 修改成功」；`GET /profile` 見新值；原密碼仍可登入；`GET /auth/me` 的 `displayName` 已是新名稱。
3. **IDOR**：同一請求夾帶 `"id": <他人 id>`、`"account": "<他人帳號>"` → 只有本人被修改。
4. **格式錯誤**：`displayName` 為 `"   "`、`email` 為 `abc`、`phone` 為 `09xx-abc` → 各 400 指出欄位，資料未變。
5. **US3 變更密碼**：帶正確 `currentPassword` 與一致合法新密碼 → 200；舊密碼登入失敗、新密碼成功；`passwordChangedAt` 為今日、`passwordRemainingDays` 為 90。
6. **密碼拒絕情境**（皆 400、整筆不寫入）：11 碼／缺大寫／缺小寫／缺數字；兩次不一致；只填其中一個；未填或錯誤的 `currentPassword`；新密碼等於目前密碼；全空白新密碼；同時改名與錯誤密碼時名稱亦未改。
7. **鎖定**：連續 5 次 `currentPassword` 錯誤 → 第 6 次即使正確也回「嘗試次數過多，請稍後再試」，同帳號登入亦被拒。
8. **US4 效期**：使 `PasswordChangedAt` 為 31 天前 → `59`／`false`；90 天前 → `0`／`true` 且仍可登入與使用本功能；NULL → 以建立日計算；設定 `PasswordPolicy:ValidDays=60` → 依 60 天計算。
9. **三入口一致**：以 `Abcdefghij1`、`abcdefghijk1`、`ABCDEFGHIJK1`、`Abcdefghijkl` 分別呼叫 `POST /users`、`PUT /users/{id}/password`、`PUT /profile` → 皆 400 且訊息一致；種子 admin 既有密碼仍可登入。
10. **操作紀錄**：查 `SysUserLog`，每個進入用例的請求各一筆（`FunctionId=PersonalData`、`Action=Modify`、`Status` 為 `Success`／`Failure`、`CreateUser` 為帳號）；格式驗證 400 的請求無紀錄；`Content` 不含密碼。查 `SysAuditLog`：`PasswordHash`／`PasswordSalt` 為遮罩值，`Phone`／`Email`／`DisplayName` 有改前改後值。Serilog 輸出不含測試密碼字串。

## Dependencies（相依）

- SPEC-004 使用者管理與登入：`User` 聚合、加鹽雜湊密碼、登入鎖定（`ILoginThrottle`，其 FR-006a）；本規格修改其 FR-013 密碼規則。見 [使用者管理與登入](../../../其他/使用者管理與登入/spec.md)。
- SPEC-005 選單導向授權模型：`MenuAccess:{menu value}` 授權。見 [選單導向授權模型](../../../其他/選單導向授權模型/spec.md)。
- SPEC-006 資料表異動紀錄：`[SensitiveData]` 遮罩、`AuditableEntityInterceptor`。見 [資料表異動紀錄](../../../其他/資料表異動紀錄/spec.md)。
- SPEC-007 CHB選單與角色種子：選單種子機制與 CHB 五角色。見 CHB選單與角色種子。
- SPEC-014 分行資料維護：分行主檔（分行代碼與名稱）。見 分行資料維護。
- 帳號維護：帳號狀態（啟用／停用）與所屬分行指派。見 SPEC-058 [帳號維護](../帳號維護/spec.md)。
- 下游：SPEC-060 使用者存取記錄查詢本規格建立的 `SysUserLog`，見 使用者存取記錄；SPEC-016 系統參數維護沿用 `SysUserLog`，見 系統參數維護。

## Risks（風險）

- `SysUserLog` 採 `bigint IDENTITY` 主鍵（DATABASE-SCHEMA §6 例外）與獨立 DbContext 寫入，見「原則符合性」Complexity Tracking；操作紀錄為盡力寫入，寫入失敗僅留 `[USER_LOG]` 錯誤日誌，不阻斷主流程。
- 密碼規則全系統提升為 12 碼：既有密碼不追溯，舊規則密碼仍可登入，直到下次變更或重設；前端新增使用者／重設密碼的提示文字需同步更新（前端團隊負責）。
- 帳號狀態在「帳號維護」引入停用機制前恆為 `Enabled`（R10）；密碼逾期僅顯示、不限制登入（FR-012），強制改密碼另需規格處理。
- 登入鎖定計數存於行程記憶體（沿用 SPEC-004 既有機制），多執行個體部署時各自計數。【待確認】是否需在部署前改為共用儲存。
- R14 修正後稽核欄位改記真實帳號，與修正前寫入的歷史資料（記為 `system`）並存。

## 實作紀錄（Archive）

- **狀態**：已實作並收斂，所有任務完成（T001–T065）。
- **主要提交**：
  - 初版：`c9e5d14` 規格 → `fc7ceb2` 設計與契約 → `c5b3f66` 任務清單（50 項）→ `1103c7a` 一致性分析修正 → `ca4c0fd` 實作 → `79be39d`／`63b253b` 收斂 T052–T053（`ICurrentUser` 回歸測試、非預期錯誤回 500）→ `62232d9`／`78de633` 收斂 T054（補 404／500 說明）→ `81ea696` 合併（2026-09-29）。
  - 2026-09-30 資料表規範對齊修訂：`b332c16` 文件修訂 → `c44f232` 收斂任務 T055–T065 → `c8aac36`／`23aa175` `SysUserLog` 五個標準欄位（T055–T057）→ `663ef25`／`7554fb2` `SysUser.BranchId` 外鍵（T058–T063）→ `f2c14b6` 密碼遮罩測試（T064）→ `e208237` 完成（T065）→ `2cb5fed` 合併。
- **審查紀錄**：規格品質檢查（原 checklists/requirements.md）第 1 輪剩 3 個待釐清標記（FR-008 密碼規則、FR-009 是否驗證目前密碼、FR-012 逾期處置範圍），第 2 輪（2026-09-29）使用者回覆後全數消除、全部項目通過；一致性分析修正見 `1103c7a`（鎖定決議、SC-007 範圍、補非預期錯誤單元測試、T034 權限前置、日誌護欄移至測試區）。
- **測試數**：2026-09-30 修訂完成時全套 179 單元 + 79 整合 + 204 功能綠。
- **原 Spec Kit 產出**：plan、research、data-model、quickstart、tasks、checklists、contracts 已併入本檔後移除；原始檔可由 `git show e208237:"specs/管理業務/系統管理作業/個人資料維護/tasks.md"` 等方式追溯。
- **TDD 紅燈證據**（2026-10-06 依憲章 2.1.0 補記；紅燈 commit 只含當時會失敗的測試，可用 `git show <commit>` 查看）：2026-09-30 修訂的紅燈 commit：`cd4816c`（T055）、`292257b`（T058）。初版實作沒有單獨提交紅燈 commit，無紀錄可補。
