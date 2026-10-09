# 資訊架構（IA）

## 1. 網站地圖

```
/                          首頁
├── /about                 關於我們
├── /services              診療服務一覽
│   ├── /gynecology        中醫婦科
│   ├── /beauty            中醫美容
│   └── /slim              減肥修身
├── /blog                  中醫資訊（第 1 頁）
│   ├── /blog/page/{n}     第 n 頁（n = 2..19）
│   └── /blog/{slug}       文章內頁
└── /contact               聯絡我們
```

## 2. URL 規則 [觀察]

| 規則 | 行為 |
|------|------|
| 無副檔名、小寫英文路徑 | `/about`、`/gynecology` |
| 結尾斜線 | `/about/` 回 200（未轉址，見已知問題） |
| `www` 子網域 | `http://www.syttcm.com/` 301 轉址 |
| HTTP | 升級至 HTTPS（HSTS） |
| Blog 分頁 | 第 1 頁為 `/blog`，第 2 頁起為 `/blog/page/{n}`；超出範圍（如 `/blog/page/99`）回 404 |
| Blog 文章 slug | 英文小寫、以 `-` 連接的長描述性 slug，常含地區詞，例：`lam-tin-beauty-breastfeeding-facial-products-baby-contact-map` |
| 不存在路徑 | 回 HTTP 404 與自訂 404 頁 |

## 3. 主導覽（Header）[觀察]

順序與連結如下（所有頁面相同；目前頁面加 `class="active"` 與 `aria-current="page"`）：

| # | 文字 | 連結 | 備註 |
|---|------|------|------|
| 1 | 首頁 | `/` | |
| 2 | 關於我們 | `/about` | |
| 3 | 診療服務 ▾ | `/services` | 下拉選單父項 |
| 3a | 中醫婦科 | `/gynecology` | 子選單 |
| 3b | 中醫美容 | `/beauty` | 子選單 |
| 3c | 減肥修身 | `/slim` | 子選單 |
| 4 | 特色優勢 | `/#why` | 首頁錨點 |
| 5 | 常見問題 | `/#faq` | 首頁錨點 |
| 6 | 中醫資訊 | `/blog` | |
| 7 | 聯絡我們 | `/contact` | |
| 8 | 立即預約 | `/contact#booking` | CTA 樣式按鈕 |

## 4. Footer 導覽 [觀察]

四欄：

1. **聖儀堂中醫診所**：一段品牌介紹文字。
2. **快速連結**：首頁 `/`、關於我們・醫師資歷 `/about`、診療服務一覽 `/services`、常見問題 `/#faq`、中醫健康資訊 `/blog`、聯絡我們・地址交通 `/contact`。
3. **診療範疇**：中醫婦科・不孕不育 `/gynecology`、懷孕安胎・產後調理 `/gynecology#postnatal`、中醫美容・中藥面膜 `/beauty`、面部經絡理療・美容針 `/beauty#needle`、減肥修身・茶療減肥 `/slim`、中醫全科・皮膚調理 `/services#general`。
4. **聯絡預約**：電話（`tel:+85255123132`）、WhatsApp、兩間分店地址、應診時間。

底部：免責聲明 + 版權列。

## 5. 頁面錨點（Section ID）清單 [觀察]

這些 ID 供頁內導航、Footer 及跨頁連結使用，新版應保留以維持既有外部連結與 SEO。

| 頁面 | Section ID |
|------|-----------|
| `/` | `top`、`services`、`gyne-detail`、`beauty-detail`、`slim-detail`、`why`、`area`、`wellness`、`faq`、`booking` |
| `/about` | `team`、`diagnosis`、`philosophy`、`qualifications`、`facility`、`scope`、`first-visit`、`faq`、`branches` |
| `/services` | `gyne`、`facial`、`slim`、`general`、`external`、`match`、`price`、`flow`、`safety`、`faq` |
| `/gynecology` | `core`、`menstrual`、`infertility`、`ivf`、`pregnancy`、`postnatal`、`menopause`、`disorders`、`oncology`、`scope`、`visit`、`faq` |
| `/beauty` | `directions`、`match`、`flow`、`mask`、`needle`、`care`、`compare`、`inner`、`price`、`faq` |
| `/slim` | `types`、`methods`、`acupoints`、`embed`、`tea`、`womb`、`postnatal`、`plan`、`lifestyle`、`plateau`、`safety`、`faq` |
| `/contact` | `booking`、`lightbox` |

> **[缺陷]** `/services`、`/gynecology`、`/beauty`、`/slim` 的頁內導航連結寫成 `/#xxx`（指向首頁），而非 `#xxx`，點擊後會跳回首頁。詳見 [known-issues.md](../08-issues/known-issues.md)。

## 6. 內部連結策略 [觀察]

- **內文情境連結**：段落中的關鍵詞直接連到相關服務頁（例如婦科段落提到「中醫美容」即連 `/beauty`）。
- **標籤雲（Keyword Pills）**：各頁底部有一組關鍵字連結（中醫婦科、婦女不孕不育、懷孕安胎、產後調理、產後扎肚、中醫美容、中藥面膜、美容針、中醫減肥、茶療減肥、中醫全科、醫師資歷、觀塘中醫地址、藍田中醫地址等），多個不同文字指向同一頁，用於 SEO 錨文字。
- **預約 CTA**：大部分頁尾 CTA 指向 `/#booking`（首頁預約表單），Header CTA 指向 `/contact#booking`。兩處表單相同。
- **Blog → 服務頁**：文章內文會連回相關服務頁與 `/about`。

## 7. 麵包屑

| 頁面 | 麵包屑 |
|------|--------|
| 首頁 | 首頁 › 觀塘中醫・藍田中醫｜中醫婦科・中醫美容・減肥修身 |
| 關於 | 首頁 / 關於我們 |
| 服務 | 首頁 / 診療服務 |
| 婦科 | 首頁 / 診療服務 / 中醫婦科 |
| 美容 | 首頁 / 診療服務 / 中醫美容・面部經絡理療 |
| 減肥 | 首頁 / 診療服務 / 修身塑形 |
| 聯絡 | 首頁 / 聯絡我們 |
| Blog 列表 | 首頁 › 中醫健康資訊 |
| Blog 文章 | 首頁 › 中醫健康資訊 › {文章標題} |

各頁同時輸出 `BreadcrumbList` JSON-LD。
