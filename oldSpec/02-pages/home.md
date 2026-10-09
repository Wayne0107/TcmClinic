# 首頁 `/`

## 1. 基本資訊

| 項目 | 內容 |
|------|------|
| URL | `/`（canonical `https://syttcm.com/`） |
| Title | 觀塘中醫｜聖儀堂中醫診所｜中醫婦科・中醫美容・減肥修身｜藍田中醫 |
| 目的 | 品牌入口、三大服務分流、在地 SEO 主頁、預約轉換 |
| 主要轉換 | 首頁預約表單 `#booking`、WhatsApp、電話 |
| CSS | `/assets/css/pages/home.css` + `site.css` |
| JSON-LD | MedicalClinic + LocalBusiness、Physician、WebSite、BreadcrumbList、FAQPage（14 題） |
| 完整文案 | [99-appendix/content-snapshots/home.txt](../99-appendix/content-snapshots/home.txt) |

## 2. 區塊順序

| # | Section（ID） | 英文小標 | 標題 | 元件／內容 |
|---|--------------|---------|------|-----------|
| 1 | Hero（`top`） | — | H1 觀塘中醫・藍田中醫 聖儀堂中醫診所 | 3 張輪播，見 [ui-components.md](../01-global/ui-components.md#1-首頁-hero-輪播hero-slider) |
| 2 | 麵包屑 | — | 首頁 › 觀塘中醫・藍田中醫｜… | |
| 3 | 統計列 | — | — | 4 個計數器：1／6／2／380 |
| 4 | 服務（`services`） | OUR SERVICES | 全面中醫診療服務 | 介紹段 + 6 張服務卡片（見下表） |
| 5 | 婦科深度內容（`gyne-detail`） | TCM GYNECOLOGY | 中醫婦科：由月經到產後的全週期調理 | 長文：月經四分型表、不孕備孕、安胎（含清胎毒 H4）、產後四階段表、扎肚時機 H4、更年期、婦科癌症扶正、免責段 |
| 6 | 美容深度內容（`beauty-detail`） | TCM BEAUTY | 中醫美容：面部經絡理療與中藥面膜 | 七步流程清單、中藥面膜成分、自製面膜注意（H4）、美容針適應症、術後護理與不適合人士 |
| 7 | 減肥深度內容（`slim-detail`） | BODY SHAPING | 減肥修身：針灸、暖宮與茶療調理 | 四體質表、針灸原理、腹部暖宮、兩款茶療、穴位按摩、平台期 |
| 8 | 關於（分欄） | ABOUT US | 潘巧儀 註冊中醫（女）專業婦科及全科 | 醫師照片 + 介紹 + 5 點清單 + 「了解更多關於我們的醫師團隊 →」`/about` |
| 9 | 特色優勢（`why`） | WHY CHOOSE US | 為何選擇聖儀堂中醫診所 | 6 張特色卡（見下表） |
| 10 | 診所影片 | CLINIC VIDEO | 診所環境影片 | `info001.mp4` |
| 11 | 醫師資歷 | QUALIFICATIONS | 醫師專業資歷 | 3 張 ★ 卡：學歷背景／教研與學術／專業專長 |
| 12 | 服務地區（`area`） | SERVICE AREA | 觀塘與藍田診所服務範圍 | H3：觀塘裕民中心診所／藍田匯景商場診所／覆蓋地區與交通／首次診症流程與收費透明度（6 步） |
| 13 | 中醫養生（`wellness`） | TCM WELLNESS | 中醫養生：香港人最需要的日常調理 | 四季養生、袪濕／睡眠／情緒、體質自我觀察三指標 + 關鍵字標籤雲 |
| 14 | FAQ（`faq`） | FAQ | 常見問題 | 14 題 `<details>`，題目見 [faq-inventory.md](../05-business-data/faq-inventory.md) + 緊急就醫提示 |
| 15 | 預約（`booking`） | APPOINTMENT | 網上預約診症 | 左：診所資訊（地址、電話、時間、WhatsApp）；右：預約表單 `home-booking-form`，見 [booking-form.md](../04-features/booking-form.md) |
| 16 | Footer | | | 共用 |

### 2.1 服務卡片（#services）

| 標籤 | 標題 | 連結 |
|------|------|------|
| 婦科博士主理 | 中醫婦科 | `/gynecology` |
| 經絡調理 | 面部經絡理療 | `/beauty` |
| 內外變美 | 減肥修身療程 | `/slim` |
| 儀容治療 | 儀容及皮膚調理 | `/services` |
| 中藥配藥 | 中醫全科診治 | `/services` |
| 觀塘・藍田 | 診所環境與位置 | `/contact` |

圖片使用 `/imges/ser001.jpg`、`ser002.jpg`、`ser003.jpg`、`ser005.jpg`、`ser201.jpg`、`store00x.jpg` 等。

### 2.2 特色優勢卡（#why）

| 圖示 | 標題 | 重點 |
|------|------|------|
| 👩‍⚕️ | 女醫師主理婦科 | 潘巧儀主理；劉國盈 30 年經驗、港大腫瘤學碩士 |
| 🌿 | 自設中藥配藥房 | 中藥齊全，可安排顆粒沖劑 |
| 🎯 | 個人化療程方案 | 不推套餐式一律處理 |
| 📍 | 觀塘・藍田兩店 | 鄰近港鐵及巴士站 |
| 📱 | 電話或 WhatsApp 預約 | 5512 3132，星期一至六應診 |
| 💬 | 學術與教學背景 | 中醫學院教授、持續進修課程導師 |

## 3. 驗收要點

- [ ] Hero 3 張輪播自動每 5 秒切換，箭頭與圓點可操作。
- [ ] 統計列進入視窗後由 0 計數至目標值。
- [ ] 主導覽「特色優勢」「常見問題」在任何頁面點擊皆能捲至首頁對應錨點。
- [ ] 預約表單送出後顯示伺服器回傳訊息，成功時清空表單。
- [ ] FAQ 14 題可個別展開收合，並輸出 FAQPage JSON-LD。
