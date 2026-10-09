# SEO：結構化資料（JSON-LD）

全部以 `<script type="application/ld+json">` 輸出，多數頁面使用 `@graph`。

## 1. 各頁輸出的類型

| 頁面 | Schema 類型 |
|------|------------|
| `/` | `MedicalClinic`+`LocalBusiness`（`#clinic`）、`Physician`（`#physician`）、`WebSite`（`#website`）、`BreadcrumbList`、`FAQPage`（14） |
| `/about` | `AboutPage`、`MedicalClinic`、2×`Physician`（含 `EducationalOccupationalCredential`×6、`CollegeOrUniversity`×3）、`WebSite`、`BreadcrumbList`、`FAQPage`（12） |
| `/services` | `CollectionPage`、`ItemList`（6×`Service`）、4×`Offer`、`MedicalClinic`、`WebSite`、`BreadcrumbList`、`FAQPage`（14） |
| `/gynecology` | `MedicalWebPage`、`MedicalTherapy`、8×`MedicalCondition`、`Physician`、`Patient`（受眾）、`MedicalClinic`、`WebSite`、`BreadcrumbList`、`FAQPage`（16） |
| `/beauty` | `MedicalWebPage`、`ItemList`（3×`Service`）、1×`Offer`（380）、6×`MedicalCondition`、`Physician`（含 `EducationalOccupationalCredential`×3）、`Patient`、`MedicalClinic`、`WebSite`、`BreadcrumbList`、`FAQPage`（16） |
| `/slim` | `MedicalWebPage`、`ItemList`（3×`Service`）、3×`Offer`（398／1880／350）、1×`MedicalCondition`、`Physician`、`Patient`、`MedicalClinic`、`WebSite`、`BreadcrumbList`、`FAQPage`（15） |
| `/contact` | **無**（聯絡頁完全沒有 JSON-LD） |
| `/blog`、`/blog/page/n` | **無** |
| `/blog/{slug}` | `BlogPosting`（無 BreadcrumbList），見 [blog-post.md](../03-blog/blog-post.md#5-blogposting-json-ld觀察) |

## 2. 診所主體（首頁）

```json
{
  "@type": ["MedicalClinic", "LocalBusiness"],
  "@id": "https://syttcm.com/#clinic",
  "name": "聖儀堂中醫診所",
  "alternateName": "SHENG YI TANG TCM",
  "url": "https://syttcm.com/",
  "telephone": "+85255123132",
  "image": "https://syttcm.com/imges/about012.jpeg",
  "logo": "https://syttcm.com/photo/logo.jpg",
  "priceRange": "$$",
  "currenciesAccepted": "HKD",
  "medicalSpecialty": ["Gynecologic", "Dermatology"],
  "address": {
    "@type": "PostalAddress",
    "streetAddress": "觀塘牛頭角道300-302號裕民中心商場地下23B",
    "addressLocality": "觀塘",
    "addressRegion": "九龍",
    "addressCountry": "HK"
  },
  "areaServed": ["觀塘", "藍田", "油塘", "九龍灣", "牛頭角"],
  "openingHoursSpecification": [
    { "dayOfWeek": ["Monday","Tuesday","Wednesday","Thursday","Friday","Saturday"], "opens": "10:30", "closes": "13:30" },
    { "dayOfWeek": ["Monday","Tuesday","Wednesday","Thursday","Friday","Saturday"], "opens": "14:30", "closes": "19:30" }
  ],
  "availableService": [ "…9 項 MedicalTherapy，見 service-catalog.md" ],
  "employee": { "@id": "https://syttcm.com/#physician" }
}
```

（`areaServed` 實際為 `Place` 物件陣列，此處簡化。）

## 3. 已知不足

- 聯絡頁（最需要 LocalBusiness 的頁面）沒有任何 JSON-LD。
- 只輸出**觀塘一個地址**；藍田分店沒有獨立的 `LocalBusiness`／`department`，不利藍田在地搜尋。
- 無 `geo` 座標、無 `hasMap`。
- Blog 列表無 `CollectionPage`；文章無 `BreadcrumbList`，`author` 為 Organization 而非醫師。
- 所有文章 `image` 相同。

## 4. 新版建議

- 以 `@graph` 輸出主診所 + 兩個分店（`department` 或兩個 `MedicalClinic`），各自地址與座標。
- Blog 文章 `author` 改為審稿醫師 `Physician`（醫療內容的 E-E-A-T）。
- 保留 FAQPage，並確保 JSON-LD 題目與畫面一致（從同一份資料產生）。
