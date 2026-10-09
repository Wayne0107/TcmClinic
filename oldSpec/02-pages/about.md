# 關於我們 `/about`

## 1. 基本資訊

| 項目 | 內容 |
|------|------|
| URL | `/about` |
| Title | 關於我們｜聖儀堂中醫診所｜潘巧儀註冊中醫・觀塘中醫・藍田中醫 |
| 目的 | 建立信任：醫師團隊、學歷、診症理念、設施、首次到診須知 |
| JSON-LD | AboutPage、2 × Physician（含 EducationalOccupationalCredential、CollegeOrUniversity）、MedicalClinic、FAQPage（12 題）、BreadcrumbList |
| 完整文案 | [99-appendix/content-snapshots/about.txt](../99-appendix/content-snapshots/about.txt) |

## 2. 區塊順序

| # | Section（ID） | 英文小標 | 標題 | 內容 |
|---|--------------|---------|------|------|
| 1 | Banner | — | H1 關於聖儀堂中醫診所｜潘巧儀註冊中醫主理 | 副標：觀塘裕民中心・藍田匯景商場｜…；麵包屑 |
| 2 | Intro 分欄 | OUR STORY | 潘巧儀註冊中醫（女） | 醫師照片 `about002.jpeg` + 3 段介紹 + 5 點清單 |
| 3 | 統計列 | — | — | 2 位註冊中醫師／30 年臨床經驗（劉醫師）／2 間分店／6 天應診 |
| 4 | 醫師團隊（`team`） | ABOUT US | 醫師團隊 | 兩位醫師卡片（見 [doctors.md](../05-business-data/doctors.md)）+ 「如何核實中醫師的註冊資格」（含三種核實方法 H4） |
| 5 | 四診（`diagnosis`） | DIAGNOSIS METHOD | 堅持「望聞問切」四診合參 | 4 步卡（01 望／02 聞／03 問／04 切）+ 5 個 H3 說明（四診合參、舌象、問診、切診、辨證論治） |
| 6 | 診症理念（`philosophy`） | OUR PHILOSOPHY | 診症理念與治療原則 | 5 個 H3（先治本再治標、不推套餐、合理預期、與西醫並行、患者參與）+ 4 張特色卡 |
| 7 | 學術 | ACADEMIC | 學術風采與教學傳承 | 論壇講者照片 `about001.jpeg` + 3 段 |
| 8 | 資歷時間軸（`qualifications`） | QUALIFICATIONS | 學歷與專業資歷 | 5 項：學歷／教研職務／學術榮譽／專業專長／團隊資歷 |
| 9 | 設施（`facility`） | OUR FACILITY | 診所環境與中藥配藥房 | 獨立診症室、中藥牆、飲片 vs 顆粒沖劑對照表（3 列）、針具衛生、無障礙 |
| 10 | 診療範疇（`scope`） | OUR SCOPE | 主要診療範疇一覽 | 4 列表格（婦科／美容／減肥／全科 → 各詳情頁）+ 2 個 H3 + 關鍵字標籤雲 |
| 11 | 首次到診（`first-visit`） | FIRST VISIT | 首次到診須知 | 預約方式、準備資料（5 項）、當日避免事項（4 項）、流程與收費、覆診 + 免責段 |
| 12 | FAQ（`faq`） | FAQ | 關於診所與醫師的常見問題 | 12 題 |
| 13 | 分店（`branches`） | OUR CLINICS | 兩間分店・歡迎預約 | 2 張分店卡（地址、電話、交通、時間、服務地區） |
| 14 | CTA | — | 歡迎預約就診 | 「立即網上預約 →」`/#booking` |

## 3. 重點內容規格

### 首次到診 — 建議準備資料
用藥清單（含劑量）、檢查報告、月經記錄（最近三次）、過敏史、孕產狀況。

### 首次到診 — 當日避免
不刮舌苔、不喝染色飲品（咖啡、紅酒、火龍果汁）、不濃妝、不空腹過久或剛吃飽、劇烈運動後休息約 15 分鐘。

### 就診流程
登記與健康問卷 → 四診辨證 → 說明體質判斷與建議方案 → 列明收費項目 → 治療及配藥 → 安排覆診。

## 4. 驗收要點
- [ ] 兩位醫師資料與 [doctors.md](../05-business-data/doctors.md) 一致。
- [ ] `#first-visit` 錨點可由 `/services` 連入。
- [ ] 統計列數值正確。
