# 相依、授權與共用元件（Dependencies & Licensing）

> 版本 1.1.0｜最後更新 2026-10-09

> 本檔為憲章 Governance「相依授權」條款的 **How**，並收錄套件管理與特定共用/工具元件的用法。

## 1. Central Package Management（CPM）

- 版本集中於 repo 根的 `Directory.Packages.props`；**個別專案不得**指定套件版本。
- 每個在此（以及 `.config/dotnet-tools.json`）新增的相依，都必須記錄於 `THIRD-PARTY-NOTICES.md`（見 §2）。
- 前端（`frontend/`）用 pnpm workspace：相依宣告在各子專案的 `package.json`，實際版本以 `frontend/pnpm-lock.yaml` 為準（`docs/FRONTEND.md` §11）。`package.json` 的直接相依同樣記錄於 `THIRD-PARTY-NOTICES.md`；遞移相依不逐一列出，改以 `pnpm licenses list --prod` 檢查授權是否符合 §2 的政策。

## 2. 相依與授權治理

### 清單（THIRD-PARTY-NOTICES.md，MUST）

- repo 根必須維護 `THIRD-PARTY-NOTICES.md`，列出每個第三方相依。
- 每筆記錄：套件/工具名稱、宣告所在 manifest（`Directory.Packages.props` / `.config/dotnet-tools.json` / `frontend/<子專案>/package.json`）、用途、授權（SPDX，如 `MIT`、`Apache-2.0`）、來源 URL。
- 依消費型別分區：**Production**（隨散布/部署產物出貨）vs **Dev/Test/Build-only**（不散布，如測試框架、變異/架構工具、linter）——散布狀態決定實際的姓名標示義務。
- 清單必須在**同一次**新增/移除/升級相依的變更中更新。缺漏或過期 = 缺陷，須於審查中擋下。

### 授權政策（MUST）

- 許可式授權——`MIT`、`Apache-2.0`、`BSD-2-Clause`、`BSD-3-Clause`、`ISC`——預先核准。
- Copyleft 或 source-available——`GPL-*`、`LGPL-*`、`MPL-2.0`，尤其 `AGPL-3.0` / `SSPL`——未經維護者於 PR 明文核可不得加入。`AGPL-3.0` / `SSPL` 在任何散布或網路託管元件中禁止，除非維護者給予書面例外。
- 授權未知、自訂或商業的相依，使用前必須審查核准。

### 姓名標示義務（澄清）

- MIT 與 Apache-2.0 的標示/通知義務在**散布**時附著，非在原始碼可見性。閉源但散布的產物仍須附必要通知；純內部或網路服務通常無需公開。無論如何維護清單，讓散布模式改變時已就緒。
- Apache-2.0 相依若附 `NOTICE` 檔，散布產物必須重現其內容。

### Greenfield（MUST）

- 範本已附與骨架相依一致的 `THIRD-PARTY-NOTICES.md`；新專案（見 `docs/INFRASTRUCTURE.md` §3）沿用此檔，之後每次增刪或升級相依都在同一次變更中更新。

## 3. SharedKernel

- 以 `Ardalis.SharedKernel` 的基底類別為基礎（`EntityBase`、`IAggregateRoot`、`DomainEventBase`、`IRepository<T>`、`LoggingBehavior`）。
- 生產專案宜將這些 fork/複製為專案自有套件（如 `YourCompany.SharedKernel`），而非相依上游 NuGet（其非官方支援，僅作 demo/範本）。

## 4. TreeCollections（MIT）— 樹狀/階層結構

用於建模階層資料（分類、組織圖、留言串、BOM）。凡功能需操作或查詢樹，一律用它，不要手刻 parent/child 導覽。

慣例：

- 可變階層用衍生自 `MutableEntityTreeNode<TNode, TId, TItem>` 的節點類；查詢路徑不可變則用 `ReadOnlyEntityTreeNode<...>`。
- 節點的 `TId` 識別選擇器必須用聚合的 Vogen 強類型 ID，不用裸原始型別。
- 從扁平清單（EF Core 載入的列）用 `AutoBuiltTreeSource` 建樹，不要手動組裝。
- 用 library 的意圖方法變更——`AddChild`、`Move`、`Detach`/`Attach`、`Reorder`——不要直接操作子集合。
- TreeCollections 型別留在 Core/UseCases 作為領域建模工具；**不得**洩漏到 Web DTO。於端點邊界把樹映射成純回應 DTO（如巢狀 record）。
- 倚賴 library 內建錯誤偵測（環路、重複 ID）為第一線；超越結構的領域不變式仍須依憲章原則強制。
- 用 LINQ 遍歷/過濾節點列舉，不要寫客製遞迴。

```csharp
// 以 Vogen 強類型 id 為鍵的領域樹節點
public sealed class CategoryNode
    : MutableEntityTreeNode<CategoryNode, CategoryId, Category>
{
    public CategoryNode(Category root) : base(c => c.Id, root) { }
}

var root = new CategoryNode(rootCategory);
root.AddChild(childCategory);                          // 結構變更
var electronics = root.First(n => n.Item.Name == "Electronics");
electronics.Move(toParent: root);                      // 移動子樹
var allLeaves = root.Where(n => !n.Children.Any());    // LINQ 遍歷
```

- 版本只在 `Directory.Packages.props` 宣告（CPM），並記錄於 `THIRD-PARTY-NOTICES.md`（MIT）。
- 官方 wiki：https://github.com/davidwest/TreeCollections/wiki

## 5. PDFsharp（MIT）＋ Noto Sans TC（OFL-1.1）— 含中文的 PDF 產生

> 範本骨架不含 PDF 功能；以下為源專案定案的選型，新專案需要產生 PDF 時沿用，並依 §1、§2 加入套件與通知。

- 需要輸出含中文的 PDF 時，使用 **PDFsharp** 並嵌入 **Noto Sans TC**。不要用 WinAnsi 標準字型（如 Helvetica），中文會變成問號。
- 字型以內嵌資源（如 `TcmClinic.Api.Infrastructure/Pdf/Fonts/NotoSansTC-Regular.ttf`）提供，透過 `IFontResolver` 回傳，不依賴伺服器安裝的字型，使 Windows／Linux 容器輸出一致。PDFsharp 的字型解析為全域設定，只註冊一次。
- 使用靜態 TrueType 版（非可變字型 VF）。PDFsharp 會以子集嵌入實際用到的字形，產出檔案小。
- 授權：PDFsharp 為 MIT；Noto Sans TC 為 SIL OFL 1.1（不在 §2 預核准清單，源專案已於 2026-10-01 核准），散布時須附 `OFL.txt`（設定隨建置輸出）。
- **不採 QuestPDF**：Community 授權限年營收 < 100 萬美元之組織，多數企業需商業授權，不符 §2 許可式授權政策。
