import { describe, expect, it } from 'vitest'
import { legacyAnchorTarget } from '~/utils/legacy-anchors'
import { isNavActive, mainNav, serviceLinks } from '~/utils/nav'

describe('legacyAnchorTarget', () => {
  // 情境：舊站服務頁的頁內錨點（如 /gynecology#postnatal）→ 改到拆出去的主題子頁。
  it('服務頁舊錨點導向主題子頁', () => {
    expect(legacyAnchorTarget('/gynecology', '#postnatal')).toBe('/gynecology/postnatal')
    expect(legacyAnchorTarget('/gynecology', '#ivf')).toBe('/gynecology/fertility')
    expect(legacyAnchorTarget('/beauty', '#needle')).toBe('/beauty/acupuncture')
    expect(legacyAnchorTarget('/slim', '#tea')).toBe('/slim/tea')
  })

  // 情境：服務一覽、關於我們、首頁的舊錨點 → 改到就診須知各頁或新頁（可帶新錨點）。
  it('其他頁舊錨點導向新頁', () => {
    expect(legacyAnchorTarget('/services', '#price')).toBe('/pricing')
    expect(legacyAnchorTarget('/services', '#external')).toBe('/general#external')
    expect(legacyAnchorTarget('/about', '#first-visit')).toBe('/visit')
    expect(legacyAnchorTarget('/', '#gyne-detail')).toBe('/gynecology')
  })

  // 情境：錨點在新版仍存在於原頁（如首頁 #faq、婦科 #core）或不認得 → 不導向。
  it('仍存在或不認得的錨點不導向', () => {
    expect(legacyAnchorTarget('/', '#faq')).toBeNull()
    expect(legacyAnchorTarget('/gynecology', '#core')).toBeNull()
    expect(legacyAnchorTarget('/gynecology', '')).toBeNull()
    expect(legacyAnchorTarget('/blog', '#price')).toBeNull()
  })

  // 情境：錨點名稱剛好是物件原型屬性（#constructor）→ 不可誤判為對照表項目。
  it('原型屬性名稱不視為舊錨點', () => {
    expect(legacyAnchorTarget('/gynecology', '#constructor')).toBeNull()
    expect(legacyAnchorTarget('/constructor', '#toString')).toBeNull()
  })
})

describe('isNavActive', () => {
  const services = mainNav.find((n) => n.to === '/services')!

  // 情境：在某科的主題子頁 → 「診療服務」選單維持高亮。
  it('主題子頁讓上層選單高亮', () => {
    expect(isNavActive(services, '/gynecology/menstrual')).toBe(true)
    expect(isNavActive(services, '/general')).toBe(true)
  })

  // 情境：路徑只是前綴相同（/blogger 對 /blog）或在別的選單下 → 不高亮。
  it('前綴相同但不是子頁時不高亮', () => {
    const blog = mainNav.find((n) => n.to === '/blog')!
    expect(isNavActive(blog, '/blogger')).toBe(false)
    expect(isNavActive(services, '/faq')).toBe(false)
  })

  // 情境：選單列出的子頁路徑都以所屬科別目錄開頭（子頁連結與目錄頁一致）。
  it('各科子頁路徑在該科目錄之下', () => {
    for (const s of serviceLinks)
      for (const c of s.children ?? []) expect(c.to.startsWith(`${s.to}/`)).toBe(true)
  })
})
