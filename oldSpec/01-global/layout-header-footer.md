# 全站版型：Header／Footer／浮動元件

所有頁面共用同一套外框（Header、Footer、WhatsApp 浮動按鈕），由伺服器端輸出。

## 1. 頁面骨架

```
<header>            固定頂部導覽列
<main / sections>   頁面內容
<footer>            四欄頁尾 + 免責聲明 + 版權
<a.wa-float>        右下角 WhatsApp 浮動按鈕
<script site.js>
```

`<body>` 帶頁面 class，例如 Blog 頁為 `class="page-blog"`。

## 2. Header [觀察]

### 2.1 結構
- 左側 **品牌區**（連結 `/`）：Logo 圖（50×50，圓形、綠色 2px 邊框）+ 雙行文字
  - 主行：「聖儀堂中醫診所」，Noto Serif TC，綠→藍漸層文字
  - 副行：「SHENG YI TANG TCM」，小字、字距 3px
- 右側 **主導覽** `<nav class="menu" id="menu" aria-label="主要導覽">`，項目見 [information-architecture.md](../00-overview/information-architecture.md#3-主導覽header觀察)
- 「診療服務」為下拉選單（`.has-sub` > `.sub-toggle` + `.submenu`）
- 最後一項「立即預約」為紅色漸層 CTA 按鈕（`.cta`）
- 行動版漢堡按鈕 `<button class="burger" id="burger" aria-label="開啟選單" aria-expanded="false">`（三條線）

### 2.2 行為
| 行為 | 規格 |
|------|------|
| 定位 | `position:fixed; top:0`，z-index 1000，白色 95% 透明 + `backdrop-filter: blur(10px)` |
| 高度 | 預設 74px；`scrollY > 50` 時縮為 66px（JS 直接改 inline style） |
| 桌面下拉 | 滑鼠 hover `.has-sub` 顯示 `.submenu` |
| 目前頁 | 對應連結加 `.active` 與 `aria-current="page"` |
| 行動版（≤900px） | 選單變為右側抽屜（寬 74%，最大 320px，由右滑入 0.35s）；漢堡按鈕切換 `.on`／`.open`，同步更新 `aria-expanded` |
| 行動版子選單 | 點「診療服務」不導頁，改為展開／收合子選單（`.has-sub.open`） |
| 行動版點連結 | 點擊一般連結後自動關閉抽屜 |

## 3. Footer [觀察]

- 背景深綠墨色 `var(--ink)`，文字淺灰綠。
- 四欄（`.fgrid`）：品牌介紹／快速連結／診療範疇／聯絡預約，內容見 IA 文件。
- 聯絡預約欄：
  - 電話：`5512 3132`（`tel:+85255123132`）
  - WhatsApp 預約（預填訊息「您好，我想預約聖儀堂中醫診所。」）
  - 📍 觀塘牛頭角道300-302號裕民中心商場地下23B
  - 📍 藍田匯景商場
  - 🕒 星期一至六：上午 10:30–13:30｜下午 14:30–19:30；星期日全日休診
- **免責聲明**（全站相同）：
  > 免責聲明：本網站所載中醫藥資訊僅供健康教育及一般參考之用，並不構成醫療診斷、治療建議或療效保證，亦不能取代註冊中醫師或註冊醫生的面診。療程效果因個人體質、病情及依從性而異。孕婦、哺乳期婦女、慢性病患者及正服用處方藥物者，應於接受任何療程前諮詢註冊中醫師。
- **版權列**：`© 2026 聖儀堂中醫診所 SHENG YI TANG TCM. 版權所有。｜觀塘中醫・藍田中醫・中醫婦科・中醫美容・減肥修身`
- ≤900px 時四欄改為單欄。

## 4. WhatsApp 浮動按鈕 [觀察]

| 項目 | 規格 |
|------|------|
| 元素 | `<a class="wa-float" target="_blank" rel="noopener" aria-label="WhatsApp 預約">💬 WhatsApp</a>` |
| 連結 | `https://wa.me/85255123132?text=您好，我想預約聖儀堂中醫診所。`（URL 編碼） |
| 位置 | 固定右下角 22px（≤600px 時 14px），z-index 950 |
| 樣式 | WhatsApp 綠 `#25d366`、膠囊形、陰影 |
| 動畫 | `waBob` 2 秒上下浮動 6px，無限循環 |
| 顯示 | 所有頁面皆顯示 |
