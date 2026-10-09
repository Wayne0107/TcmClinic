# ADR-009 密碼雜湊改 PBKDF2，既有雜湊於登入成功時自動轉換

| 項目 | 內容 |
|------|------|
| 狀態 | Accepted |
| 日期 | 2026-10-08 |
| 決策者 | Wayne（維護者；授權 AI 直接決定實作細節） |
| 出處 | 開發規範裁決 2026-10-08（PRJ-2026-0008）第 48 題；[SPEC-004](../../specs/其他/使用者管理與登入/spec.md) Clarifications Session 2026-10-08、變更（Delta）2026-10-08；specs/README.md 延後事項 DEF-008 |
| 取代 | [SPEC-004](../../specs/其他/使用者管理與登入/spec.md) 設計決策 R2（密碼雜湊：加鹽 SHA-256） |

> 📦 本 ADR 隨範本沿用自源專案（CHB），決策本身仍適用於範本。背景、影響與相關連結中提到的舊系統、分行、企業、開狀申請、系統參數等屬源專案業務，相關規格、資料表與 migration 範本已移除；以骨架程式碼與測試為準。

## 背景（Context）

SPEC-004（2026-08）依使用者明示需求，以「每帳號隨機 salt + SHA-256」保存密碼（FR-007、設計決策 R2），並登記為已接受的安全風險：SHA-256 是快速雜湊，即使加 salt，遭離線暴力破解時的保護仍遠弱於專為密碼設計的慢雜湊。當時以 `IPasswordHasher` 介面封裝，保留日後替換的空間，並登記延後事項 DEF-008。

2026-10-08 逐條比對 UDS 標準後，使用者裁決撤銷這個例外；同時裁決專案規範「PBKDF2 即可，不限定 Argon2id／bcrypt／scrypt」維持不變（第 8 題）。系統已有使用中的帳號，雜湊值是 SHA-256 格式，換演算法時必須處理。

## 考慮過的選項

演算法：

1. **PBKDF2-HMAC-SHA512，100,000 次迭代**：與 ASP.NET Core Identity v3 預設相同；.NET 內建 `Rfc2898DeriveBytes.Pbkdf2`，不需新增套件。
2. **PBKDF2-HMAC-SHA256，600,000 次迭代**（OWASP 2023 建議值）：安全邊際較大，但每次登入的運算時間約為選項 1 的數倍。
3. **Argon2id／bcrypt**：需新增第三方套件；專案規範已裁決不限定這兩種。

既有雜湊的轉換：

- **A. 登入成功時自動重新雜湊**：使用者無感；舊雜湊在轉換前仍可驗證。
- **B. 要求所有使用者重設密碼**：轉換最快、最徹底，但所有人都要走重設流程，造成營運負擔。
- **C. 雙重雜湊（以 SHA-256 結果再做 PBKDF2）**：可一次轉換全部資料，但演算法變得非標準，日後維護困難。

## 決策結果

採用 **選項 1 ＋ A**：

- 新雜湊以 PBKDF2-HMAC-SHA512、100,000 次迭代、16-byte 隨機 salt、32-byte 輸出計算。
- 雜湊字串格式為 `pbkdf2-sha512$<迭代次數>$<Base64 雜湊>`（65 字元，沿用 `PasswordHash nvarchar(88)`）；salt 照舊存在 `PasswordSalt`。資料表結構不變。
- 沒有前綴的雜湊視為舊的 SHA-256 格式，仍可驗證。
- 登入驗證成功後，若儲存的是舊格式，或迭代次數低於現行值，就以現行參數重新雜湊並保存。
- 重新雜湊**只換雜湊與 salt，不變更 `PasswordChangedAt`**：使用者沒有真的改密碼，密碼效期（SPEC-013 FR-010/011）不得因此重置。
- 新增、重設、本人變更密碼一律產生新格式。

**理由**：

- 不新增套件，參數與 ASP.NET Core Identity 相同，屬業界常見設定，符合專案規範「PBKDF2 即可」。
- 自動轉換不打擾使用者；常用的帳號在第一次登入後就換成新格式。
- 雜湊字串帶迭代次數，日後要調高時只改常數，既有雜湊同樣會在登入時自動升級。

## 後果

- **好**：
  - 密碼儲存改為慢雜湊，離線暴力破解的成本大幅提高。
  - 遷移不需要使用者配合，也不需要資料表 migration。
  - 日後調整迭代次數有固定的升級路徑。
- **壞**：
  - 長期沒登入的帳號會一直保留 SHA-256 雜湊，直到下次登入或管理者重設密碼。需要時可另外查出仍為舊格式的帳號（`PasswordHash` 不以 `pbkdf2-` 開頭），要求重設密碼。
  - 每次登入多約數十毫秒的雜湊運算。
  - 第一次以舊雜湊登入時會多一次寫入，留下一筆稽核紀錄（`PasswordHash` 欄位依既有遮罩規則不記值）。

## 連結

- 規格：[SPEC-004](../../specs/其他/使用者管理與登入/spec.md) FR-007、FR-007a、AC-13、AC-31
- 規範：[docs/SECURE-CODING.md](../SECURE-CODING.md) §6
- 延後事項：[specs/README.md](../../specs/README.md) DEF-008
- 程式：`backend/src/TcmClinic.Api.Infrastructure/Data/Security/Pbkdf2PasswordHasher.cs`、`backend/src/TcmClinic.Api.UseCases/Auth/Login.cs`、`backend/src/TcmClinic.Api.Core/UserAggregate/User.cs`（`UpgradePasswordHash`）
