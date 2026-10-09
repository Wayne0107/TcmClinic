# 舊站已知問題與改版建議

嚴重度：🔴 高（功能錯誤／明顯影響 SEO 或轉換）　🟠 中　🟢 低

## 1. 功能缺陷

| # | 嚴重度 | 問題 | 位置 | 建議 |
|---|-------|------|------|------|
| F1 | 🔴 | **頁內導航錨點錯誤**：連結寫成 `/#id`，點擊會跳到首頁而不是本頁 section | `/services`（8 個：gyne、facial、slim、general、external、match、price、safety）、`/gynecology`（10 個：core、menstrual、infertility、ivf、pregnancy、postnatal、menopause、disorders、oncology、visit）、`/beauty`（9 個：directions、match、flow、mask、needle、care、compare、inner、price）、`/slim`（11 個：types、methods、acupoints、embed、tea、womb、postnatal、plan、lifestyle、plateau、safety） | 改為 `#id` |
| F2 | 🔴 | 服務頁頁內導航的「常見問題」寫成 `/#faq`，會跳到**首頁** FAQ 而非本頁 FAQ（首頁剛好有 `#faq`，所以不會報錯，但內容錯） | services、gynecology、beauty、slim | 改為 `#faq` |
| F3 | 🟠 | 預約日期可選星期日（休診日）及公眾假期 | 預約表單 | 前端停用星期日，後端再驗證 |
| F4 | 🟠 | 統計數字 HTML 初始為 0，依賴 JS 才顯示正確數值；SEO 與無 JS 環境看到 0 | 首頁、關於頁 | HTML 輸出真實數值，動畫只作加強 |
| F5 | 🟢 | 燈箱 `<img src="">` 空 src | 聯絡頁 | 不輸出空 src |
| F6 | 🟢 | WhatsApp 分店預填文字未 URL 編碼 | 聯絡頁 | 統一 encode |

## 2. 內容／資料問題

| # | 嚴重度 | 問題 | 位置 | 建議 |
|---|-------|------|------|------|
| C1 | 🔴 | **藍田分店沒有完整地址**（只寫「藍田匯景商場」，無舖號），JSON-LD 也只有觀塘地址 | 全站 | 向業主取得藍田完整地址（含舖號），並加入 JSON-LD |
| C2 | 🟠 | 錯字「血瘦」應為「血瘀」 | about（1 處）、beauty（4 處）、gynecology（1 處） | 修正 |
| C3 | 🟠 | 地點名稱不一致：文字「匯景商場」，地圖查詢「匯景廣場」 | 聯絡頁 | 統一 |
| C4 | 🟠 | 潘醫師港大學歷寫法不一（「深造碩士（臨床婦科）」vs「臨床婦科學深造課程」） | 首頁 vs 關於頁 | 與醫師確認正式名稱 |
| C5 | 🟠 | 相同 FAQ 在多頁重複且答案略有差異 | 6 個頁面 | 建立單一 FAQ 資料源 |
| C6 | 🟠 | 價格在多頁與 meta description 硬寫 | services、beauty、slim、首頁、meta | 集中管理價格資料 |
| C7 | 🟢 | 範例文章內 H2 順序「第五組詞」排在「第四組詞」前 | `/blog/persistent-vulvar-itch-…` | 內容校對 |
| C8 | 🟢 | 劉國盈醫師無照片 | 關於頁 | 補充 |

## 3. SEO 問題

| # | 嚴重度 | 問題 | 建議 |
|---|-------|------|------|
| S1 | 🟠 | 聯絡頁沒有任何 JSON-LD | 加入 LocalBusiness（兩分店） |
| S2 | 🟠 | 所有 Blog 文章共用同一張 og:image，且無封面圖 | 支援每篇封面圖 |
| S3 | 🟠 | Blog 無分類、無搜尋；文章無 BreadcrumbList；author 為 Organization | 加入分類、麵包屑 JSON-LD、醫師作者 |
| S4 | 🟢 | 結尾斜線 `/about/` 回 200 而非 301 | 統一 301 到無斜線 |
| S5 | 🟢 | sitemap 首頁 `https://syttcm.com` 與 canonical `https://syttcm.com/` 不一致 | 統一 |
| S6 | 🟢 | 聯絡頁 title 分隔符號混用半形「 \| 」 | 統一全形「｜」 |
| S7 | 🟢 | 資料夾名 `/imges/` 拼字錯誤 | 新版改 `/images/`，舊路徑 301 |

## 4. 安全／隱私

| # | 嚴重度 | 問題 | 建議 |
|---|-------|------|------|
| P1 | 🟠 | 沒有私隱政策頁，表單收集健康相關資料 | 新增私隱政策並於表單連結 |
| P2 | 🟠 | 公開頁面也發出名為 `SYTTCM_ADMIN` 的 session cookie | 前後台 session 分離 |
| P3 | 🟢 | `X-Powered-By: PHP/8.2.13` 透露版本 | 移除 |
| P4 | 🟢 | 無 Content-Security-Policy | 加入 CSP |

## 5. 效能／無障礙

詳見 [performance.md](../07-non-functional/performance.md) 與 [accessibility.md](../07-non-functional/accessibility.md)。主要：

- 🟠 HTML 全部 `no-store` 無法快取；每頁 CSS 重複基礎樣式。
- 🟠 圖片未最佳化、多數未 lazy load、無尺寸屬性。
- 🟠 輪播無暫停、未支援 `prefers-reduced-motion`。
- 🟠 桌面下拉選單無法用鍵盤展開；燈箱無焦點管理。

## 6. UX 建議

- 服務頁 CTA「立即網上預約」都連到首頁 `/#booking`，使用者會離開目前頁面。建議每頁內嵌預約表單或開啟對話框，並預選對應服務項目。
- 404 頁沒有 Header／Footer，建議加上。
- 預約後沒有確認編號或 Email／SMS 通知。
