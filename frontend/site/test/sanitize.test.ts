// @vitest-environment node
// 消毒函式在伺服器端（SSR）以 jsdom 執行；happy-dom 的 HTML 解析與 DOMPurify 不相容，所以本檔改用 node 環境。
import { describe, expect, it } from 'vitest'
import { sanitizeArticleHtml } from '../app/utils/sanitize'

describe('sanitizeArticleHtml', () => {
  // 情境：文章內文混入 script 與事件屬性（XSS）→ 必須移除，只保留文章標籤（docs/FRONTEND.md §7）。
  it('移除 script 與事件屬性', () => {
    const html = sanitizeArticleHtml(
      '<p onclick="alert(1)">內文</p><script>alert(1)</script><img src=x onerror=alert(1)>',
    )
    expect(html).toBe('<p>內文</p>')
  })

  // 情境：連結使用 javascript: 協定 → 移除 href，避免點擊執行程式碼。
  it('移除 javascript: 連結', () => {
    expect(sanitizeArticleHtml('<a href="javascript:alert(1)">按此</a>')).toBe('<a>按此</a>')
  })

  // 情境：站內與 https 連結、標題是正常內容 → 保留。
  it('保留站內連結、外部 https 連結與標題', () => {
    const html =
      '<h2>重點</h2><p><a href="/booking">預約</a><a href="https://example.com">來源</a></p>'
    expect(sanitizeArticleHtml(html)).toBe(html)
  })
})
