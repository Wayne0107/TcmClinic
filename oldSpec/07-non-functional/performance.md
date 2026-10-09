# 非功能需求：效能

## 1. 資源概況 [觀察]

| 資源 | 大小 | 快取 |
|------|------|------|
| 首頁 HTML | 約 58KB（gzip 後約 20KB） | `no-store`（每次重新請求） |
| 服務頁 HTML | 約 56–61KB | `no-store` |
| 頁面 CSS（每頁一份） | 11–14KB，gzip | `max-age=2592000`（30 天）+ ETag |
| `site.css` | 2.2KB | 30 天 |
| `site.js` | 5.3KB | 30 天 |
| 圖片（例 about012.jpeg） | 約 380KB | `max-age=31536000`（1 年） |
| 影片 info001.mp4 | 約 1.1MB | `preload="none"` |
| Google Fonts | Noto Serif TC 4 字重 + Noto Sans TC 4 字重（CJK 字型體積大） | Google CDN |

## 2. 觀察到的效能問題

1. **CSS 重複**：每頁的 `pages/*.css` 都內含全套基礎樣式（Header、Footer、按鈕等），換頁時無法共用快取。
2. **HTML 不可快取**：因 session／CSRF token，所有頁面 `no-store`，CDN 與瀏覽器都無法快取 HTML。
3. **圖片未最佳化**：JPEG 原圖數百 KB，未使用 WebP/AVIF、`srcset` 或多尺寸。
4. **Lazy loading 少用**：只有少數圖片加 `loading="lazy"`（about 2、services 4、其餘頁 0–1）；聯絡頁 12 張相片全部即時載入。
5. **圖片無尺寸屬性**：除 Logo 外，`<img>` 多無 `width`／`height`，容易造成版面位移（CLS）。
6. **Hero 背景圖**：以 CSS `background-image` 載入，瀏覽器較晚發現，影響 LCP。
7. **字型**：8 個 CJK 字重，載入量大；已使用 `display=swap` 與 preconnect。
8. **快取版本**：資源以 `?v=1` 作版本號，需手動更新。

## 3. 新版目標建議

| 指標 | 目標 |
|------|------|
| LCP（行動 4G） | < 2.5s |
| CLS | < 0.1 |
| INP | < 200ms |
| 首頁總傳輸（不含影片） | < 1MB |

做法：共用 CSS 拆分、圖片轉 WebP 並提供多尺寸、Hero 首圖 preload、非首屏圖片 lazy、字型縮減字重或子集化、公開頁不發 session 以便快取 HTML。
