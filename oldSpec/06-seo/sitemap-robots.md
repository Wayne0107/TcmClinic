# SEO：sitemap.xml 與 robots.txt

## 1. robots.txt [觀察]

```
User-agent: *
Allow: /
Disallow: /admin
Disallow: /preview/

Sitemap: https://syttcm.com/sitemap.xml
```

## 2. sitemap.xml [觀察]

動態產生，包含靜態頁與所有已發佈文章。

| URL | changefreq | priority | lastmod 來源 |
|-----|-----------|----------|-------------|
| `https://syttcm.com`（無結尾斜線） | weekly | 1.0 | 推測為首頁內容最後更新時間 |
| `/about`、`/services`、`/gynecology`、`/beauty`、`/slim`、`/contact` | monthly | 0.8 | 各頁最後修改時間 |
| `/blog/{slug}`（282 筆，新到舊） | monthly | 0.7 | 文章發佈時間（含 +08:00 時區） |
| `/blog`（位於檔案最後） | weekly | 0.8 | 最新文章發佈時間 |

注意：
- 首頁 sitemap 寫 `https://syttcm.com`，canonical 寫 `https://syttcm.com/`，兩者不一致。
- 未收錄 `/blog/page/{n}` 分頁（合理）。
- 單一 sitemap 檔；文章數超過數千時需改用 sitemap index。

## 3. 轉址與 Canonical

| 規則 | 現況 |
|------|------|
| HTTP → HTTPS | 是（HSTS max-age=31536000; includeSubDomains） |
| www → 非 www | 301 |
| 結尾斜線 `/about/` | 回 200，未轉址；canonical 仍指向 `/about` |
| 每頁 canonical | 指向自身完整 URL |

## 4. 改版必須保留

- 全部現有 URL（7 個靜態頁 + 282 篇文章 + 分頁）須維持相同路徑或提供 301。
- robots 中後台與預覽路徑的排除。
- sitemap 自動包含新文章。
