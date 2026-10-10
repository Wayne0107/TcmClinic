import DOMPurify from 'isomorphic-dompurify'

/** 醫師專欄內文允許的標籤；其餘一律移除（docs/FRONTEND.md §7 XSS） */
const ARTICLE_TAGS = ['p', 'h2', 'h3', 'ul', 'ol', 'li', 'a', 'strong', 'em', 'br']

/**
 * 消毒文章 HTML 後才可交給 v-html。後端儲存時同樣要消毒，前端消毒不能取代後端。
 * 只保留 href（限 http(s) 與站內路徑）與 id（目錄錨點）。
 */
export function sanitizeArticleHtml(html: string): string {
  return DOMPurify.sanitize(html, {
    ALLOWED_TAGS: ARTICLE_TAGS,
    ALLOWED_ATTR: ['href', 'id'],
    ALLOWED_URI_REGEXP: /^(?:https?:\/\/|\/(?!\/))/i,
  })
}
