/**
 * 前台與 mock API 共用的資料型別。
 * 對應未來 .NET 後端 API 的回應格式；欄位命名採 camelCase。
 *
 * 內容原則：畫面文字一律取自舊站 syttcm.com 原文（逐字），不自行撰寫；
 * 舊站沒有的資料（醫師應診時間等）以選填欄位表示，沒有資料就不顯示。
 *
 * 文字欄位標記 `RichText` 者支援兩種行內語法（由 <RichText> 元件安全轉換，不接受 HTML）：
 *   **粗體**
 *   [連結文字](/站內路徑 或 https://外部網址)
 */
export type RichText = string

/** 圖片：src 為 /images/ 底下的檔名主幹（不含寬度與副檔名），由 <ResponsiveImage> 組出 srcset */
export interface ImageRef {
  src: string
  alt: string
}

export interface Seo {
  /** 完整 <title>（舊站原文，不再自動加上診所名稱） */
  title: string
  description: string
}

export interface LinkItem {
  label: string
  to: string
}

// ── 診所與分店 ─────────────────────────────────────

export interface OpeningPeriod {
  /** 0 = 星期日 … 6 = 星期六 */
  days: number[]
  open: string // "10:30"
  close: string // "13:30"
}

export interface Branch {
  id: number
  slug: string
  name: string // 觀塘裕民中心
  shortName: string // 觀塘
  label: string // 舊站分店卡上的小標，如「觀塘店」
  address: string
  district: string
  transport: string
  mapQuery: string
  image: ImageRef
  whatsappText: string
}

export interface ClinicInfo {
  name: string
  nameEn: string
  /** 頁尾介紹（舊站原文） */
  description: string
  phone: string // 顯示用 "5512 3132"
  phoneE164: string // "+85255123132"
  whatsapp: string // "85255123132"
  whatsappDefaultText: string
  hours: OpeningPeriod[]
  /** 應診時間顯示文字（舊站原文） */
  hoursText: string
  hoursNote: string
  closedNote: string
  branches: Branch[]
  disclaimer: string
  copyright: string
}

// ── 醫師 ───────────────────────────────────────────

export interface DoctorSchedule {
  branchId: number
  /** 0 = 星期日 … 6 = 星期六 */
  day: number
  slot: 'morning' | 'afternoon'
}

export interface Doctor {
  slug: string
  name: string
  /** 舊站標題，如「潘巧儀 註冊中醫（女）」 */
  displayName: string
  gender: 'female' | 'male'
  /** 舊站副標，如「專業婦科及全科｜中醫美容與塑形」 */
  headline: string
  photo?: ImageRef
  /** 舊站醫師介紹段落（原文） */
  bio: RichText[]
  /** 舊站醫師介紹下的條列（原文） */
  highlights: string[]
  isLead: boolean
  /** 個人頁的補充段落（如學術背景） */
  sections?: ContentSection[]
  /** 應診時間：舊站沒有提供，待診所提供後才顯示 */
  schedule?: DoctorSchedule[]
}

// ── 內容區塊（各頁共用） ───────────────────────────

export type ContentBlock =
  | { type: 'p'; text: RichText; tone?: 'muted' | 'note' }
  | { type: 'h3'; text: string }
  | { type: 'h4'; text: string }
  | { type: 'list'; ordered?: boolean; items: { title?: string; text: RichText }[] }
  | { type: 'table'; caption?: string; head: string[]; rows: RichText[][] }
  | { type: 'callout'; tone: 'info' | 'warn'; title?: string; text: RichText }
  | { type: 'steps'; items: { title: string; text: RichText }[] }
  | { type: 'cards'; items: { title: string; text: RichText; tag?: string }[] }
  | { type: 'tags'; items: string[] }
  | { type: 'links'; items: LinkItem[] }
  | { type: 'prices'; items: { label: string; price: string }[] }

/** 區塊中插入的元件（醫師卡、分店卡、影片、相簿、預約表單） */
export type SectionSlot = 'doctors' | 'branches' | 'video' | 'gallery' | 'booking'

export interface ContentSection {
  /** 錨點 ID，沿用舊站以保留外部連結；舊站沒有的可省略 */
  id?: string
  /** 舊站英文小標（原文，如 GYNECOLOGY）；舊站沒有就省略 */
  en?: string
  title: string
  /** 標題下的導言（原文） */
  lead?: RichText
  blocks: ContentBlock[]
  /** 在 blocks 之後插入的元件 */
  slot?: SectionSlot
}

export interface Faq {
  /** 全站 FAQ 的識別碼（/faq 頁的錨點） */
  id?: string
  q: string
  a: RichText
}

/** /faq 頁的一個分類 */
export interface FaqCategory {
  slug: string
  name: string
  items: Faq[]
}

export interface FaqPageContent {
  seo: Seo
  banner: { en?: string; title: string; lead?: string }
  categories: FaqCategory[]
  note?: string
}

export interface FaqSection {
  en?: string
  title: string
  lead?: string
  items: Faq[]
  /** FAQ 後的提示（原文） */
  note?: string
}

export interface CtaContent {
  title: string
  text?: string
  label: string
}

// ── 服務 ───────────────────────────────────────────

export type ServiceSlug = 'gynecology' | 'beauty' | 'slim' | 'general'

export interface ServiceSummary {
  slug: ServiceSlug
  path: string // /gynecology
  name: string
  /** 舊站卡片／區塊說明（原文） */
  summary: string
  image: ImageRef
  /** 舊站標籤（原文） */
  tags: string[]
}

/** 目錄頁上的主題卡片（連到子頁；也可連到其他科的子頁，如減肥的「產後修身」） */
export interface ServiceTopicLink {
  slug: string
  path: string
  title: string
  /** 卡片說明：該主題第一段的導言（原文） */
  summary: RichText
}

/** 服務目錄頁（hub）：簡介、主題卡片、概覽段落 */
export interface ServiceDetail {
  slug: ServiceSlug
  path: string
  name: string
  seo: Seo
  /** 頁首：舊站 H1、副標與導言（原文） */
  hero: { en?: string; title: string; subtitle?: string; lead: RichText[]; image: ImageRef }
  /** 頁首價格（舊站原文） */
  prices: { label: string; price: string }[]
  /** 主題卡片上方的導言（原文） */
  topicsLead?: RichText
  topics: ServiceTopicLink[]
  sections: ContentSection[]
  faq: FaqSection
  cta: CtaContent
  /** 預約表單預選的 service id（見 BookingServiceOption） */
  bookingServiceId: number
}

/** 服務主題子頁，如 /gynecology/menstrual */
export interface ServiceTopic {
  service: ServiceSlug
  serviceName: string
  servicePath: string
  slug: string
  path: string
  title: string
  en?: string
  lead?: RichText
  seo: Seo
  sections: ContentSection[]
  faq: FaqSection
  cta: CtaContent
  bookingServiceId: number
}

export interface BookingServiceOption {
  id: number
  name: string
}

export interface ServicesOverview {
  seo: Seo
  banner: { title: string; lead: string }
  services: ServiceSummary[]
  sections: ContentSection[]
  faq: FaqSection
  cta: CtaContent
}

/** 一般資訊頁：就診須知（/visit、/pricing、/safety）、醫師團隊、聯絡我們 */
export interface InfoPage {
  seo: Seo
  banner: { en?: string; title: string; lead?: RichText }
  /** 頁首下方、各段落之前的內容 */
  intro?: ContentBlock[]
  sections: ContentSection[]
  faq?: FaqSection
  cta?: CtaContent
}

// ── 頁面內容（首頁、關於我們） ─────────────────────

export interface StatItem {
  value: string
  label: string
}

export interface HomeContent {
  seo: Seo
  hero: { tag: string; title: string; brand: string; lead: string; ctaLabel: string }
  stats: StatItem[]
  services: {
    en: string
    title: string
    lead: string
    cards: { tag: string; title: string; text: string; linkLabel: string; to: string }[]
  }
  about: { en: string; title: string; paragraphs: RichText[]; bullets: string[]; linkLabel: string }
  why: {
    en: string
    title: string
    lead: string
    items: { icon: string; title: string; text: string }[]
  }
  faq: FaqSection
  /** 頁尾預約區 */
  cta: CtaContent
}

export interface AboutContent {
  seo: Seo
  banner: { title: string; lead: string }
  stats: StatItem[]
  sections: ContentSection[]
  faq: FaqSection
  cta: CtaContent
}

// ── 中醫資訊（Blog） ───────────────────────────────

export interface PostCategory {
  slug: string
  name: string
}

export interface PostSummary {
  slug: string
  title: string
  excerpt: string
  category: PostCategory
  /** 舊站文章作者為診所（Organization），沒有個別醫師 */
  authorName: string
  cover: ImageRef
  publishedAt: string // ISO 8601 +08:00
  readingMinutes: number
}

export interface Post extends PostSummary {
  seoTitle: string
  metaDescription: string
  /** 經後端清洗過的 HTML（只含 p、h2、h3、ul、ol、li、a、strong、em） */
  contentHtml: string
  updatedAt: string
}

/** 分頁回應（ADR-007、docs/FRONTEND.md §5.4） */
export interface Paged<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

// ── 優惠與好評 ─────────────────────────────────────

/** 優惠：取自舊站收費表原文（項目、內容說明、參考收費） */
export interface Offer {
  id: string
  title: string
  description: string
  price: string
  serviceSlug?: ServiceSlug
  bookingServiceId?: number
}

export interface Review {
  id: string
  name: string
  topic: string
  text: string
}

// ── 預約 ───────────────────────────────────────────

export interface AppointmentRequest {
  name: string
  phone: string
  branchId: number
  date: string // YYYY-MM-DD
  slot: 'morning' | 'afternoon'
  serviceId: number
  doctorSlug?: string
  notes?: string
  consent: boolean
  website?: string // 蜜罐
  sourcePage: string
}

export interface AppointmentResponse {
  ok: boolean
  message: string
  reference?: string
  errors?: Partial<Record<keyof AppointmentRequest, string>>
}
