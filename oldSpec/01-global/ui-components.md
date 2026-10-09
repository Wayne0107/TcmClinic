# 共用 UI 元件

以下元件在多個頁面重複使用。JS 行為全部來自 `/assets/js/site.js`。

## 1. 首頁 Hero 輪播（Hero Slider）

| 項目 | 規格 |
|------|------|
| 位置 | 首頁頂部，`margin-top:74px`，高度 78vh（最小 480px） |
| 張數 | 3 張，背景圖分別為 `about012.jpeg`、`about005.jpeg`、`about014.jpeg` |
| 每張內容 | 標籤（綠色膠囊）、標題（第 1 張為 H1，其餘 H2）、說明、紅色 CTA 按鈕 |
| 遮罩 | 110° 深綠漸層遮罩（72% → 25%） |
| 切換 | 淡入淡出 1.1s + 縮放（1.08→1）6s |
| 自動播放 | 每 5 秒切換；手動切換後重設計時器 |
| 控制 | 左右箭頭（‹ ›）、圓點（JS 動態產生，`aria-label="第N張"`） |
| 無障礙 | 每張 `role="group"` + `aria-label` |

三張內容：
1. 標籤「註冊中醫潘巧儀主理」／H1「觀塘中醫・藍田中醫 聖儀堂中醫診所」／CTA「歡迎預約就診 →」→ `/#booking`
2. 標籤「專業中醫婦科」／「中醫婦科 守護女性全週期」／CTA「了解婦科服務 →」→ `/gynecology`
3. 標籤「中醫美容 · 減肥修身」／「面部經絡理療 由內而外健康變美」／CTA「查看療程詳情 →」→ `/beauty`

## 2. 內頁 Banner

非首頁的頁面頂部使用 Banner：H1 標題 + 一行副標 + 麵包屑。

## 3. 數字統計列（Stats Bar／Counter）

| 項目 | 規格 |
|------|------|
| 樣式 | 綠→藍漸層背景，4 欄（≤900px 2 欄，≤560px 1 欄），數字 Noto Serif TC 2.6rem 粗體 |
| 動畫 | 進入視窗 40% 時，數字由 0 增至 `data-n` 值，約 60 步、每步 25ms，只播一次 |
| 首頁數值 | 1 婦科博士研究生主理／6 星期一至六應診／2 觀塘・藍田兩間診所／380 面部經絡理療試做價 |
| 關於頁數值 | 2 位註冊中醫師／30 年臨床經驗（劉醫師）／2 間分店服務社區／6 天應診（星期一至六） |

> 初始 HTML 顯示「0」，若 JS 未執行或不支援 IntersectionObserver 則永遠顯示 0（見已知問題）。

## 4. 捲動淡入（Reveal）

- 元素加 `.reveal`：初始 `opacity:0; translateY(40px)`。
- IntersectionObserver（threshold 0.12）進入視窗時加 `.show`，0.8s 過場，只觸發一次。
- 不支援 IntersectionObserver 時直接全部顯示。

## 5. 服務卡片（Cards）

- 3 欄網格（≤900px 2 欄，≤560px 1 欄），白底圓角 18px、陰影，hover 上移 10px。
- 卡片內容：圖片區（190px，左下紅色小標籤）+ H3 + 說明 + 漸層「了解更多 →」按鈕。
- 整張卡為 `<a class="card">` 連結。

## 6. 特色優勢卡（Why Cards）

圖示（emoji）+ H3 + 說明。首頁 6 張、關於頁 4 張。≤900px 2 欄、≤600px 1 欄。

## 7. 資料表格

用於體質分型、產後階段、收費、療程對照等。標準 `<table>`，表頭第一列。各頁表格內容詳見頁面規格。

## 8. FAQ 手風琴

| 項目 | 規格 |
|------|------|
| 元素 | 原生 `<details>` / `<summary>`，問題文字包在 `<h3>` 內，答案在 `.ans` |
| 圖示 | 收合顯示「＋」，展開顯示「－」 |
| 樣式 | 白底卡片、圓角 14px，最大寬 980px 置中 |
| 預設 | 全部收合 |
| SEO | 同頁輸出 `FAQPage` JSON-LD |

## 9. 關鍵字標籤雲（Keyword Pills）

各頁底部一組膠囊形連結，導向各服務頁與聯絡頁，用作內部連結與 SEO 錨文字。

## 10. 頁內導航（Page TOC）

服務詳情頁（services／gynecology／beauty／slim）在 intro 下方有一列膠囊連結，跳至頁內各 section。

> **[缺陷]** 連結寫成 `/#id`，實際跳回首頁。新版應改為 `#id`。

## 11. 圖片燈箱（Lightbox）— 聯絡頁

| 項目 | 規格 |
|------|------|
| 觸發 | 點擊 `#clinicGallery figure` |
| 顯示 | 全螢幕黑色 88% 遮罩，圖片最大 92vw／85vh，下方顯示 figcaption 或 alt |
| 控制 | ×（關閉）、‹ ›（上下張，循環）、點遮罩關閉 |
| 鍵盤 | Esc 關閉、← → 切換 |
| 捲動 | 開啟時 `body.style.overflow='hidden'` |

## 12. 影片區塊

- `<video controls preload="none" poster="/imges/about014.jpeg">`，來源 `/imges/info001.mp4`（約 1.1MB）。
- 首頁「診所環境影片」與聯絡頁「診所影片」使用同一影片。
- 不支援時顯示「您的瀏覽器不支援影片播放。」

## 13. 頁尾 CTA 區塊

大部分內頁底部有 H2 + 一句說明 + 「立即網上預約 →」按鈕（連 `/#booking`）。

## 14. 醫療免責提示（段落內）

服務頁與文章內多處插入小字免責段落（`.medical-disclaimer` 或 `.medical-note`），內容與 Footer 免責聲明類似。Blog 文章的 `.medical-note` 為淡黃底、左側橘色邊框。

## 15. 分頁（Pagination）

Blog 列表使用。列出全部頁碼 1..N（無上一頁／下一頁），目前頁加 `.active` 與 `aria-current="page"`，`<nav aria-label="分頁">`。
