# 附錄

## content-snapshots/

2026-10-09 從線上頁面擷取的文字內容，供改版時搬移文案。

| 檔案 | 來源 |
|------|------|
| home.txt | `/` |
| about.txt | `/about` |
| services.txt | `/services` |
| gynecology.txt | `/gynecology` |
| beauty.txt | `/beauty` |
| slim.txt | `/slim` |
| contact.txt | `/contact` |
| blog-list-page1.txt | `/blog` |
| blog-post-sample.txt | `/blog/persistent-vulvar-itch-lichen-sclerosus-lesion-language-guide` |

### 閱讀方式

由腳本將 HTML 轉為文字，格式如下：

| 標記 | 意義 |
|------|------|
| `=== HEAD ===` 段 | `<title>`、`<meta>`、`<link>` 原文 |
| `JSONLD: {...}` | 該頁 JSON-LD 原文 |
| `=== BODY ===` 段 | 頁面內容 |
| `[H1]`～`[H4]` | 標題層級 |
| `{A:文字 -> 連結}` | 超連結 |
| `<!-- ... -->` | 原始 HTML 註解（多為 section 名稱） |
| `\| \| a \| b` | 表格列 |
| `- ` | 清單項目 |

注意：Header 導覽與 Footer 在每個檔案中重複出現。

## sitemap-2026-10-09.xml

擷取當日的完整 sitemap（7 個靜態頁、`/blog` 與 282 篇文章 URL）。改版時用來核對 URL 是否全部保留或已設定 301。
