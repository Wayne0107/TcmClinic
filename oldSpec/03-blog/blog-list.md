# 中醫資訊列表 `/blog`、`/blog/page/{n}`

## 1. 基本資訊

| 項目 | 內容 |
|------|------|
| URL | 第 1 頁 `/blog`；第 n 頁 `/blog/page/{n}`（n ≥ 2） |
| Title | 第 1 頁：`中醫健康資訊 Blog｜聖儀堂中醫診所`；第 n 頁：`中醫健康資訊 Blog｜第 {n} 頁｜聖儀堂中醫診所` |
| Description | 聖儀堂中醫診所分享中醫婦科、中醫美容、產後調理、健康養生及減肥修身資訊。（各頁相同） |
| Canonical | 每頁指向自己（`/blog/page/2` canonical 為 `/blog/page/2`） |
| Robots | index,follow |
| JSON-LD | 無 |
| CSS | `pages/home.css` 基礎 + `blog.css` |
| body class | `page-blog` |

## 2. 版面

```
Header
.blog-hero      英文小標 HEALTH JOURNAL / H1 中醫健康資訊 / 分享中醫婦科、美容、養生及體重管理知識。
麵包屑          首頁 › 中醫健康資訊
.blog-grid      文章卡片網格（3 欄 → ≤900px 2 欄 → ≤620px 1 欄）
.pagination     頁碼
Footer
```

## 3. 文章卡片 `article.blog-card.reveal`

| 欄位 | 元素 | 說明 |
|------|------|------|
| 封面圖 | `img`（220px 高，object-fit cover） | CSS 已支援，**但目前所有文章皆無封面圖**，卡片只有文字 |
| 發佈日期 | `<time datetime="YYYY-MM-DD">` | 顯示格式「2026年10月9日」 |
| 標題 | `<h2><a href="/blog/{slug}">` | 文章 H1 標題（非 SEO title） |
| 摘要 | `<p>` | 約 100–150 字摘要（與 meta description 不同欄位） |
| 閱讀全文 | `<a class="more">閱讀全文 →</a>` | 連文章頁 |

卡片 hover 上移 7px。

## 4. 排序與分頁 [觀察]

| 規則 | 值 |
|------|-----|
| 排序 | 發佈時間由新到舊 |
| 每頁筆數 | 15 |
| 總數（擷取日） | 282 篇，共 19 頁（最後一頁 12 篇） |
| 分頁元件 | 列出所有頁碼 1..N，無上一頁／下一頁，無省略號 |
| 超出範圍 | 404 |
| 空狀態 | CSS 定義 `.blog-empty`（白底卡片置中），推測無文章時顯示 [推論] |

## 5. 缺少的功能（舊站沒有）

- 分類／標籤篩選
- 搜尋
- `rel="prev"`／`rel="next"`
- 列表頁結構化資料（如 `CollectionPage`／`ItemList`）

## 6. 驗收要點
- [ ] 第 1 頁不出現 `/blog/page/1`（直接為 `/blog`）。
- [ ] 每頁 15 篇，依發佈時間倒序。
- [ ] 頁碼數量 = ceil(總篇數 / 15)。
- [ ] 不存在頁碼回 404。
