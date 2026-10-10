import { describe, expect, it } from 'vitest'
import { safeHref, trailingSlashRedirect } from '#shared/utils/url'

describe('trailingSlashRedirect', () => {
  // 情境：一般頁面結尾多了斜線 → 導向無斜線版本並保留查詢字串。
  it('去掉結尾斜線並保留查詢字串', () => {
    expect(trailingSlashRedirect('/about/', '?a=1')).toBe('/about?a=1')
    expect(trailingSlashRedirect('/blog/page/2//', '')).toBe('/blog/page/2')
  })

  // 情境：首頁、API、無結尾斜線 → 不導向。
  it('首頁、API 與正常網址不導向', () => {
    expect(trailingSlashRedirect('/', '')).toBeNull()
    expect(trailingSlashRedirect('/api/posts/', '')).toBeNull()
    expect(trailingSlashRedirect('/about', '')).toBeNull()
  })

  // 情境：路徑開頭是多個斜線或反斜線（如 //evil.com/）→ 導向目標仍是站內路徑，不可變成 //evil.com（開放重新導向）。
  it('開頭多個斜線不會導向外站', () => {
    expect(trailingSlashRedirect('//evil.com/', '')).toBe('/evil.com')
    expect(trailingSlashRedirect('/\\evil.com/', '')).toBe('/evil.com')
    expect(trailingSlashRedirect('///evil.com//', '')).toBe('/evil.com')
  })
})

describe('safeHref', () => {
  // 情境：站內路徑、錨點、https、電話、WhatsApp → 允許。
  it('允許站內與白名單協定', () => {
    for (const href of [
      '/booking',
      '#faq',
      'https://wa.me/85212345678',
      'tel:+85255123132',
      'mailto:a@b.hk',
    ]) {
      expect(safeHref(href)).toBe(href)
    }
  })

  // 情境：javascript:、data:、協定相對網址、http 明碼 → 拒絕（內容日後由後台輸入，避免 XSS 與外導）。
  it('拒絕危險或非白名單的網址', () => {
    for (const href of [
      'javascript:alert%281%29',
      'JavaScript:alert(1)',
      'data:text/html,x',
      '//evil.com',
      '/\\evil.com',
      'http://example.com',
      'vbscript:x',
    ]) {
      expect(safeHref(href)).toBeNull()
    }
  })
})
