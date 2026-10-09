# 網站概覽

## 1. 網站目的

聖儀堂中醫診所的官方前台網站，主要目標：

1. **取得預約**：網上預約表單、電話、WhatsApp 三種渠道（全站多處 CTA 導向預約）。
2. **在地 SEO**：以「觀塘中醫」「藍田中醫」「觀塘美容」「藍田減肥」等地區＋服務關鍵字爭取搜尋排名。
3. **建立專業信任**：公開醫師學歷、教研職務、核實註冊資格的方法、收費原則與禁忌事項。
4. **內容行銷**：中醫資訊 Blog 每日發佈健康教育文章（截至擷取日共 282 篇）。

## 2. 品牌資訊 [觀察]

| 項目 | 內容 |
|------|------|
| 中文名稱 | 聖儀堂中醫診所 |
| 英文名稱 | SHENG YI TANG TCM |
| 網域 | https://syttcm.com |
| Logo | `/photo/logo.jpg`（圓形顯示，亦作 favicon 與 apple-touch-icon） |
| 主理醫師 | 潘巧儀 註冊中醫（女） |
| 定位標語 | 專業中醫婦科及全科，兼理中醫美容與減肥修身 |
| 語言 | 繁體中文（香港），`og:locale=zh_HK`，`inLanguage=zh-Hant-HK` |
| 版權字樣 | © 2026 聖儀堂中醫診所 SHENG YI TANG TCM. 版權所有。 |

## 3. 目標使用者

| 族群 | 主要需求 | 對應頁面 |
|------|----------|----------|
| 觀塘區上班族 | 午膳／下班時段的美容、減肥、肩頸痛、失眠 | `/beauty`、`/slim`、`/services` |
| 藍田區家庭客 | 備孕、安胎、產後調理、產後扎肚 | `/gynecology` |
| 備孕／IVF 女性 | 分階段調理、配合西醫療程 | `/gynecology#infertility`、`#ivf` |
| 更年期／婦科癌症康復者 | 扶正調理、與西醫協作 | `/gynecology#menopause`、`#oncology` |
| 男士 | 暗瘡、應酬型肥胖（FAQ 明確表示不限性別） | `/beauty`、`/slim` |

服務地區（JSON-LD `areaServed`）：觀塘、藍田、油塘、九龍灣、牛頭角。內文另提及秀茂坪、彩雲、麗港城、匯景花園、康栢。

## 4. 三大服務主線 + 全科

1. **中醫婦科**：月經、帶下、備孕／不孕、IVF/IUI 輔助、安胎、產後調理與扎肚、更年期、婦科雜病、婦科癌症扶正。
2. **中醫美容**：面部經絡理療、中藥面膜（倒膜）、美容針（美顏針）、刮痧撥筋。
3. **減肥修身**：針灸減肥、穴位埋線、耳豆、茶療、腹部暖宮、產後修身。
4. **中醫全科與皮膚**：內科雜病、痛症針灸、濕疹／牛皮癬等皮膚病、腫瘤輔助調理。

詳見 [05-business-data/service-catalog.md](../05-business-data/service-catalog.md)。

## 5. 技術堆疊（觀察與推論）

| 層級 | 內容 | 依據 |
|------|------|------|
| Web Server | Apache/2 | [觀察] `Server` 標頭 |
| 後端 | PHP 8.2.13，伺服器端渲染（SSR）每頁完整 HTML | [觀察] `X-Powered-By` 標頭 |
| Session | Cookie `SYTTCM_ADMIN`（Secure、HttpOnly、SameSite=Lax），公開頁面亦會發出 | [觀察] |
| 前端 JS | 單一檔案 `/assets/js/site.js?v=1`，原生 JS，無框架、無 jQuery | [觀察] |
| CSS | `/assets/css/pages/{page}.css?v=1`（每頁一份，內含全站基礎樣式）+ `/assets/css/site.css?v=1`（共用補充）+ `/assets/css/blog.css?v=1`（Blog） | [觀察] |
| 字型 | Google Fonts：Noto Serif TC（400/600/700/900）、Noto Sans TC（300/400/500/700） | [觀察] |
| 圖片／影片 | `/imges/`（拼字錯誤，非 images）、`/photo/logo.jpg` | [觀察] |
| 後台 | `/admin`（301 轉址，robots 禁止）、`/preview/`（robots 禁止），推測為 CMS 管理 Blog 文章與預約紀錄 | [推論] |
| 預約 API | `POST /appointment/submit`，回傳 JSON `{ ok, message }` | [觀察] 由 site.js 得知 |
| 第三方 | 無 Analytics／追蹤碼、無地圖嵌入、無聊天外掛 | [觀察] 原始碼中未出現 |

## 6. 頁面數量

- 靜態頁：7 頁（首頁、關於、服務、婦科、美容、減肥、聯絡）
- Blog 列表：`/blog` + `/blog/page/2` ~ `/blog/page/19`（每頁 15 篇）
- Blog 文章：282 篇（2026-01-01 起，約每日一篇）
- 錯誤頁：404
