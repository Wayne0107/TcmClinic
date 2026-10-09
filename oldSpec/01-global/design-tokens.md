# 設計 Token 與響應式規則

## 1. 色彩 [觀察]

定義於每頁 CSS 的 `:root`：

| Token | 值 | 用途 |
|-------|-----|------|
| `--green` | `#5cb85c` | 主色、標籤、Logo 邊框 |
| `--green-d` | `#3d8b3d` | 深綠：標題、連結、啟用狀態 |
| `--blue` | `#29abe2` | 輔色、漸層終點 |
| `--blue-d` | `#1c7fb0` | 深藍：英文小標、日期 |
| `--red` | `#c0392b` | CTA 按鈕、卡片標籤 |
| `--red-d` | `#922b21` | CTA 漸層深色 |
| `--ink` | `#2c3e34` | 內文、Footer 背景 |
| `--paper` | `#f7faf7` | 頁面背景 |
| `--line` | `#e3ece4` | 邊框、分隔線 |
| `--shadow` | `0 10px 35px rgba(44,62,52,.12)` | 卡片陰影 |

其他固定色：WhatsApp `#25d366`；錯誤提示底 `#fff0ee`／字 `#a72d20`；醫療提示底 `#fff8e8`／邊框 `#e2a332`。

常用漸層：
- 品牌字／按鈕：`linear-gradient(90deg|135deg, var(--green), var(--blue))`
- CTA：`linear-gradient(135deg, var(--red), var(--red-d))`
- 統計列：`linear-gradient(135deg, var(--green-d), var(--blue-d))`
- Blog Hero：`linear-gradient(135deg, #eef7ef, #e6f3fa)`

## 2. 字型

| 用途 | 字型 |
|------|------|
| 內文 | "Noto Sans TC", sans-serif（300/400/500/700），行高 1.8 |
| 標題 h1–h4、`.serif` | "Noto Serif TC", serif（400/600/700/900） |

字級：Hero 標題 `clamp(2rem, 5vw, 3.4rem)`；Blog 標題 `clamp(2rem, 5vw, 3.1rem)`。

## 3. 版面

| 項目 | 值 |
|------|-----|
| 容器 `.container` | 最大寬 1200px，左右 padding 20px |
| Section padding | 桌面約 90px 上下；≤560px 為 54px |
| 圓角 | 卡片 18px、FAQ 14px、按鈕膠囊 30–50px |
| Header 高 | 74px（捲動後 66px） |
| 平滑捲動 | `html{scroll-behavior:smooth}` |

英文小標慣例：每個 section 標題上方有全大寫英文小字（例 `OUR SERVICES`、`WHY CHOOSE US`、`FAQ`、`APPOINTMENT`）。

## 4. 斷點

| 斷點 | 變化 |
|------|------|
| `max-width: 900px` | 主選單變為右側抽屜、顯示漢堡按鈕；卡片／統計 3–4 欄 → 2 欄；左右分欄（`.split`、預約區 `.bk-wrap`、Footer `.fgrid`）→ 單欄；Blog 網格 3 → 2 欄 |
| `max-width: 620px` | Blog 網格 → 1 欄 |
| `max-width: 600px` | 特色卡 → 1 欄；WhatsApp 浮動按鈕縮小 |
| `max-width: 560px` | 卡片、統計、表單雙欄（`.two`）→ 1 欄；section padding 縮小 |

## 5. 動態效果

| 效果 | 規格 |
|------|------|
| Reveal | translateY(40px)→0，0.8s `cubic-bezier(.2,.8,.2,1)` |
| 卡片 hover | translateY(-10px)（Blog 卡 -7px） |
| CTA hover | translateY(-2~-3px) |
| WhatsApp | 2s 上下浮動 |
| Hero | 1.1s 淡入 + 6s 縮放 |

> 舊站未處理 `prefers-reduced-motion`（見已知問題）。
