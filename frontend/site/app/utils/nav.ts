// 網站導覽結構（單一來源）：頁首選單、頁尾連結、「就診須知」分頁列共用。
// 子頁路徑須與 server/mock/services/* 的 topics 一致（sitemap 由資料產生）。

export interface NavLink {
  label: string
  to: string
  children?: NavLink[]
}

/** 「就診須知」底下的頁面 */
export const visitLinks: NavLink[] = [
  { label: '首次就診流程', to: '/visit' },
  { label: '收費一覽', to: '/pricing' },
  { label: '安全與禁忌', to: '/safety' },
  { label: '常見問題', to: '/faq' },
]

/** 「診療服務」：各科目錄頁與其主題子頁 */
export const serviceLinks: NavLink[] = [
  {
    label: '中醫婦科',
    to: '/gynecology',
    children: [
      { label: '月經調理', to: '/gynecology/menstrual' },
      { label: '不孕不育・IVF／IUI', to: '/gynecology/fertility' },
      { label: '懷孕安胎', to: '/gynecology/pregnancy' },
      { label: '產後調理與扎肚', to: '/gynecology/postnatal' },
      { label: '更年期・婦科雜病', to: '/gynecology/menopause' },
      { label: '婦科癌症扶正', to: '/gynecology/oncology' },
    ],
  },
  {
    label: '中醫美容',
    to: '/beauty',
    children: [
      { label: '美顏針', to: '/beauty/acupuncture' },
      { label: '面部經絡理療', to: '/beauty/facial' },
      { label: '中藥面膜', to: '/beauty/herbal-mask' },
    ],
  },
  {
    label: '減肥修身',
    to: '/slim',
    children: [
      { label: '針灸減肥・穴位埋線', to: '/slim/acupuncture' },
      { label: '腹部暖宮', to: '/slim/womb-warming' },
      { label: '茶療減肥', to: '/slim/tea' },
    ],
  },
  { label: '中醫全科與皮膚', to: '/general' },
]

// 導覽文字沿用舊站 syttcm.com；「醫師團隊」「就診須知」「最新優惠」為新版新增。
// 舊站的「首頁」「特色優勢」「常見問題」不列入主選單（Logo 即回首頁，常見問題移到「就診須知」）。
export const mainNav: NavLink[] = [
  {
    label: '關於我們',
    to: '/about',
    children: [
      { label: '診所簡介', to: '/about' },
      { label: '醫師團隊', to: '/doctors' },
    ],
  },
  { label: '診療服務', to: '/services', children: serviceLinks },
  { label: '就診須知', to: '/visit', children: visitLinks },
  { label: '中醫資訊', to: '/blog' },
  { label: '最新優惠', to: '/offers' },
  { label: '聯絡我們', to: '/contact' },
]

/** 目前路徑是否在此連結（或其子頁）之下 */
export function isNavActive(link: NavLink, path: string): boolean {
  const own = path === link.to || (link.to !== '/' && path.startsWith(`${link.to}/`))
  return own || (link.children?.some((c) => isNavActive(c, path)) ?? false)
}
