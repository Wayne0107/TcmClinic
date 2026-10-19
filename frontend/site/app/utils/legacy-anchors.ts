// 舊網址錨點對照：2026-10-10 網站架構改為「目錄頁＋主題子頁」，原本頁內的段落搬到獨立頁面。
// 外部或書籤連到舊錨點（如 /gynecology#postnatal）時，由 middleware/legacy-anchors.global.ts 導向新頁。
// 錨點不會送到伺服器，只能在瀏覽器端處理。仍留在原頁的錨點（如首頁 #faq）不列入。

const MOVED: Record<string, Record<string, string>> = {
  '/': {
    'gyne-detail': '/gynecology',
    'beauty-detail': '/beauty',
    'slim-detail': '/slim',
    wellness: '/blog/tcm-wellness-daily-care',
    area: '/contact',
    booking: '/booking',
  },
  '/about': {
    team: '/doctors',
    qualifications: '/doctors#qualifications',
    scope: '/services',
    'first-visit': '/visit',
  },
  '/services': {
    gyne: '/gynecology',
    facial: '/beauty',
    slim: '/slim',
    general: '/general',
    external: '/general#external',
    price: '/pricing',
    flow: '/visit',
    safety: '/safety',
  },
  '/gynecology': {
    menstrual: '/gynecology/menstrual',
    infertility: '/gynecology/fertility',
    ivf: '/gynecology/fertility',
    pregnancy: '/gynecology/pregnancy',
    postnatal: '/gynecology/postnatal',
    menopause: '/gynecology/menopause',
    disorders: '/gynecology/menopause',
    oncology: '/gynecology/oncology',
    visit: '/visit',
  },
  '/beauty': {
    directions: '/beauty/acupuncture',
    needle: '/beauty/acupuncture',
    care: '/beauty/acupuncture',
    compare: '/beauty/acupuncture',
    flow: '/beauty/facial',
    mask: '/beauty/herbal-mask',
    inner: '/blog/skin-problems-root-causes',
    price: '/pricing',
  },
  '/slim': {
    acupoints: '/slim/acupuncture',
    embed: '/slim/acupuncture',
    tea: '/slim/tea',
    womb: '/slim/womb-warming',
    postnatal: '/gynecology/postnatal',
    lifestyle: '/blog/weight-loss-diet-exercise-sleep',
    plateau: '/blog/weight-loss-plateau-rebound',
    safety: '/safety',
  },
}

/** 舊錨點對應的新網址；不需導向時回傳 null */
export function legacyAnchorTarget(path: string, hash: string): string | null {
  const id = hash.replace(/^#/, '')
  if (!id || !Object.hasOwn(MOVED, path)) return null
  const page = MOVED[path]!
  return Object.hasOwn(page, id) ? page[id]! : null
}
