# 舊版前台網站規格（oldSpec）

> 來源：https://syttcm.com/ （聖儀堂中醫診所 SHENG YI TANG TCM）
> 擷取日期：2026-10-09
> 方法：逆向分析線上前台的 HTML、CSS、JS、sitemap、robots.txt 與 HTTP 回應標頭。
> 範圍：僅限公開前台。後台（`/admin`、`/preview/`）無法存取，相關描述皆標註為「推論」。

## 標註慣例

| 標記 | 意義 |
|------|------|
| **[觀察]** | 直接由線上頁面原始碼或回應確認 |
| **[推論]** | 由可見行為推斷，未能直接驗證（例如後端邏輯） |
| **[缺陷]** | 舊站已知問題，新版應修正，詳見 [08-issues/known-issues.md](08-issues/known-issues.md) |

## 目錄結構

| 資料夾 | 內容 |
|--------|------|
| [00-overview/](00-overview/) | 網站概覽、資訊架構（Sitemap、導覽、URL 規則） |
| [01-global/](01-global/) | 全站共用版型（Header/Footer）、UI 元件、設計 Token 與響應式規則 |
| [02-pages/](02-pages/) | 各靜態頁面規格：首頁、關於我們、診療服務、中醫婦科、中醫美容、減肥修身、聯絡我們、404 |
| [03-blog/](03-blog/) | 中醫資訊（Blog）列表頁、文章頁、內容策略 |
| [04-features/](04-features/) | 功能規格：網上預約表單、聯絡渠道（電話／WhatsApp／地圖） |
| [05-business-data/](05-business-data/) | 業務資料：診所與分店、醫師、收費、服務項目分類、FAQ 清單 |
| [06-seo/](06-seo/) | Meta 標籤、結構化資料（JSON-LD）、sitemap 與 robots |
| [07-non-functional/](07-non-functional/) | 安全性、效能、無障礙、醫療內容合規 |
| [08-issues/](08-issues/) | 舊站已知問題與改版建議 |
| [99-appendix/](99-appendix/) | 各頁面文字內容快照（供改版搬移文案使用） |

## 頁面總覽

| URL | 頁面 | 規格檔 |
|-----|------|--------|
| `/` | 首頁 | [02-pages/home.md](02-pages/home.md) |
| `/about` | 關於我們 | [02-pages/about.md](02-pages/about.md) |
| `/services` | 診療服務一覽 | [02-pages/services.md](02-pages/services.md) |
| `/gynecology` | 中醫婦科 | [02-pages/gynecology.md](02-pages/gynecology.md) |
| `/beauty` | 中醫美容 | [02-pages/beauty.md](02-pages/beauty.md) |
| `/slim` | 減肥修身 | [02-pages/slim.md](02-pages/slim.md) |
| `/contact` | 聯絡我們 | [02-pages/contact.md](02-pages/contact.md) |
| `/blog`、`/blog/page/{n}` | 中醫資訊列表 | [03-blog/blog-list.md](03-blog/blog-list.md) |
| `/blog/{slug}` | 中醫資訊文章 | [03-blog/blog-post.md](03-blog/blog-post.md) |
| 其他不存在路徑 | 404 頁 | [02-pages/error-404.md](02-pages/error-404.md) |
