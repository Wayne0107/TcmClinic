# SEO：Meta 標籤

## 1. 全站共通 `<head>` [觀察]

```html
<html lang="zh-Hant-HK">
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width, initial-scale=1.0">
<link rel="icon" href="/photo/logo.jpg" type="image/jpeg">
<link rel="apple-touch-icon" href="/photo/logo.jpg">
<title>…</title>
<meta name="description" content="…">
<meta name="keywords" content="…">            <!-- 靜態頁才有 -->
<link rel="canonical" href="https://syttcm.com/…">
<meta name="robots" content="index,follow,max-image-preview:large">
<meta name="author" content="註冊中醫 潘巧儀">
<meta name="geo.region" content="HK">
<meta name="geo.placename" content="觀塘, 藍田, 香港">
<meta property="og:type" content="website|article">
<meta property="og:locale" content="zh_HK">
<meta property="og:site_name" content="聖儀堂中醫診所">
<meta property="og:title" content="（同 title）">
<meta property="og:description" content="（同 description）">
<meta property="og:image" content="…">
<meta property="og:url" content="（同 canonical）">
<meta name="twitter:card" content="summary_large_image">
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Noto+Serif+TC:wght@400;600;700;900&family=Noto+Sans+TC:wght@300;400;500;700&display=swap" rel="stylesheet">
```

## 2. 各頁 Meta

| 頁面 | Title | Description 字數 | Keywords 數 | og:image |
|------|-------|-----------------|------------|----------|
| `/` | 觀塘中醫｜聖儀堂中醫診所｜中醫婦科・中醫美容・減肥修身｜藍田中醫 | 128 | 23 | about012.jpeg |
| `/about` | 關於我們｜聖儀堂中醫診所｜潘巧儀註冊中醫・觀塘中醫・藍田中醫 | 103 | 13 | about002.jpeg |
| `/services` | 診療服務一覽｜中醫婦科・中醫美容・減肥修身｜觀塘中醫・藍田中醫 | 128 | 16 | ser001.jpg |
| `/gynecology` | 中醫婦科｜月經調理．不孕不育．懷孕安胎．產後調理｜觀塘中醫・藍田中醫 | 129 | 23 | ser001.jpg |
| `/beauty` | 中醫美容｜美顏針．中藥面膜．面部經絡理療｜觀塘美容・藍田美容 | 123 | 17 | ser201.jpg |
| `/slim` | 中醫減肥・減肥修身｜針灸減肥．茶療減肥．腹部暖宮｜觀塘減肥・藍田減肥 | 124 | 15 | ser003.jpg |
| `/contact` | 聯絡我們 \| 聖儀堂中醫診所｜觀塘裕民中心地址及預約 | 89 | 6 | about012.jpeg |
| `/blog` | 中醫健康資訊 Blog｜聖儀堂中醫診所 | 36 | — | about012.jpeg |
| `/blog/page/{n}` | 中醫健康資訊 Blog｜第 {n} 頁｜聖儀堂中醫診所 | 36 | — | about012.jpeg |
| `/blog/{slug}` | {SEO 標題}｜聖儀堂中醫診所 | 約 100 | — | about012.jpeg（全部相同） |

## 3. Title 命名規則（觀察）

- 分隔符號以全形「｜」為主；聯絡頁混用半形「 | 」（不一致）。
- 服務頁模式：`{服務名}｜{子項目以「．」分隔}｜{地區}{服務}`。
- 地區關鍵字放在 title 首或尾：首頁以「觀塘中醫」開頭、「藍田中醫」結尾。

## 4. 主要目標關鍵字

| 頁面 | 關鍵字 |
|------|--------|
| 首頁 | 觀塘中醫、藍田中醫、觀塘美容、藍田美容、觀塘減肥、藍田減肥、中醫婦科、中醫美容、中藥面膜、美容針、美顏針、中醫減肥、茶療減肥、產後扎肚、懷孕安胎、婦女不孕不育 |
| 婦科 | 中醫婦科、月經調理、痛經、崩漏、IVF中醫調理、清胎毒、產後扎肚、更年期、PCOS、子宮內膜異位症、女中醫師 |
| 美容 | 中醫美容、面部經絡理療、美顏針、韓式針雕、中藥倒膜、面部刮痧、玉石撥筋、黃褐斑 |
| 減肥 | 中醫減肥、針灸瘦身、穴位埋線、耳豆療法、腹部暖宮、易瘦體質 |

## 5. 標題階層

- 每頁恰好一個 `<h1>`。
- Section 標題用 H2，子題 H3，補充 H4。
- FAQ 問題以 H3 包在 `<summary>` 內。
- 首頁 Hero 第 1 張為 H1，第 2、3 張為 H2。
