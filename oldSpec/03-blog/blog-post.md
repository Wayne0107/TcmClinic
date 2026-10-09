# 中醫資訊文章頁 `/blog/{slug}`

## 1. 基本資訊

| 項目 | 內容 |
|------|------|
| URL | `/blog/{slug}` |
| 範例 | `/blog/persistent-vulvar-itch-lichen-sclerosus-lesion-language-guide` |
| Title | `{SEO 標題}｜聖儀堂中醫診所`（SEO 標題較 H1 短，例：「外陰長期痕與白斑傷口：症狀用語指南」） |
| Description | 獨立撰寫的 meta description（與列表摘要不同） |
| og:type | `article` |
| og:image | 全部文章共用 `https://syttcm.com/imges/about012.jpeg` |
| JSON-LD | `BlogPosting`（見下）；**無** BreadcrumbList |
| 完整範例 | [99-appendix/content-snapshots/blog-post-sample.txt](../99-appendix/content-snapshots/blog-post-sample.txt) |

## 2. 版面

```
Header
麵包屑            首頁 › 中醫健康資訊 › {文章標題}
article.blog-article（最大寬 900px）
  header.article-head
    H1 {文章標題}
    <time datetime="YYYY-MM-DD">發布：YYYY年M月D日</time>
  [img.article-cover]   CSS 支援（最大高 520px），目前文章皆未使用
  div.prose.article-content    白底卡片，內文 HTML
  div.medical-note     「本文只作一般健康教育用途，不能取代註冊中醫師或醫生的面診、診斷與治療。」
  p.article-back       ← 返回中醫健康資訊（/blog）
Footer
```

## 3. 內文格式 [觀察]

- 由 `<p>`、`<h2>`、`<h3>` 組成；未見圖片、表格、清單（CSS 已為 `.article-content img` 預備樣式）。
- 內部連結使用 `<a class="inline">`，常見目標：`/gynecology`、`/beauty`、`/slim`、`/about`、`/contact#booking`。
- 文章結尾通常引導「預約面診」並強調就醫警號。
- 無作者署名區塊、無相關文章、無分享按鈕、無留言。

## 4. 推論之文章資料模型 [推論]

由頁面輸出欄位反推後台 CMS 應有：

| 欄位 | 型別 | 用途 |
|------|------|------|
| `slug` | string（英文小寫、`-` 分隔） | URL |
| `title` | string | H1、列表卡片標題、JSON-LD headline |
| `seo_title` | string | `<title>`、og:title |
| `meta_description` | string | meta／og description |
| `excerpt` | string | 列表卡片摘要、JSON-LD description |
| `content` | HTML | 內文 |
| `cover_image` | string（可空） | 列表卡片圖、文章封面（目前皆空） |
| `published_at` | datetime（+08:00） | 排序、顯示日期、datePublished、sitemap lastmod |
| `updated_at` | datetime | dateModified |
| `status` | draft/published | `/preview/` 路徑推測為草稿預覽 |

## 5. BlogPosting JSON-LD [觀察]

```json
{
  "@context": "https://schema.org",
  "@type": "BlogPosting",
  "headline": "{title}",
  "description": "{excerpt}",
  "image": "https://syttcm.com/imges/about012.jpeg",
  "datePublished": "2026-10-09T14:13:00+08:00",
  "dateModified": "2026-10-09T14:13:00+08:00",
  "author": { "@type": "Organization", "name": "聖儀堂中醫診所" },
  "publisher": {
    "@type": "Organization",
    "name": "聖儀堂中醫診所",
    "logo": { "@type": "ImageObject", "url": "https://syttcm.com/photo/logo.jpg" }
  },
  "mainEntityOfPage": "https://syttcm.com/blog/{slug}"
}
```

## 6. 驗收要點
- [ ] 不存在的 slug 回 404。
- [ ] 每篇文章皆有 medical-note 免責提示。
- [ ] 文章 `<title>` 與 H1 可分開設定。
- [ ] 發佈日期顯示格式「發布：YYYY年M月D日」。
