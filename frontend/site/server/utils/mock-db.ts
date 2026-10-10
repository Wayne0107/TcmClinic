// mock 資料存取層：集中在此，日後換成呼叫 .NET API 時只需改這裡或改 runtimeConfig.apiBase。
import type { Doctor, Faq, FaqPageContent, Post, PostSummary } from '#shared/types/api'
import postsJson from '../mock/posts.json'
import { doctors as doctorList } from '../mock/doctors'
import { faqCategories, faqNote, faqs } from '../mock/faqs'
import { doctorSections } from '../mock/pages/info'
import { beauty, beautyTopics } from '../mock/services/beauty'
import { general } from '../mock/services/general'
import { gynecology, gynecologyTopics } from '../mock/services/gynecology'
import { slim, slimTopics } from '../mock/services/slim'

export const posts = (postsJson as Post[]).toSorted((a, b) =>
  b.publishedAt.localeCompare(a.publishedAt),
)

export const serviceDetails = { gynecology, beauty, slim, general } as const

/** 服務主題子頁，鍵為服務 slug */
export const serviceTopics = {
  gynecology: gynecologyTopics,
  beauty: beautyTopics,
  slim: slimTopics,
} as const

export const doctors: Doctor[] = doctorList.map((d) =>
  doctorSections[d.slug] ? { ...d, sections: doctorSections[d.slug] } : d,
)

/** 常見問題頁：全站 FAQ 依分類列出 */
export function faqPage(): FaqPageContent {
  return {
    seo: {
      title: '常見問題',
      description: faqs
        .slice(0, 3)
        .map((f) => f.q)
        .join(''),
    },
    banner: { en: 'FAQ', title: '常見問題' },
    categories: faqCategories.map((c) => ({
      ...c,
      items: faqs.filter((f) => f.category === c.slug).map(({ id, q, a }): Faq => ({ id, q, a })),
    })),
    note: faqNote,
  }
}

export function toSummary({
  contentHtml: _contentHtml,
  seoTitle: _seoTitle,
  metaDescription: _metaDescription,
  updatedAt: _updatedAt,
  ...summary
}: Post): PostSummary {
  return summary
}
