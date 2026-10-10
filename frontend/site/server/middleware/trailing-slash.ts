import { trailingSlashRedirect } from '#shared/utils/url'

// 網址結尾斜線統一 301 到無斜線版本（如 /about/ → /about），避免同一頁面有兩個網址（舊站已知問題 S4）
export default defineEventHandler((event) => {
  const url = getRequestURL(event)
  const target = trailingSlashRedirect(url.pathname, url.search)
  if (target) return sendRedirect(event, target, 301)
})
