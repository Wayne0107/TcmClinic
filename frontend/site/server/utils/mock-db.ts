// mock 資料存取層：集中在此，日後換成呼叫 .NET API 時只需改這裡或改 runtimeConfig.apiBase。
import type { Post, PostSummary } from '#shared/types/api'
import postsJson from '../mock/posts.json'
import { gynecology } from '../mock/services/gynecology'
import { beauty } from '../mock/services/beauty'
import { slim } from '../mock/services/slim'

export const posts = (postsJson as Post[]).toSorted((a, b) =>
  b.publishedAt.localeCompare(a.publishedAt),
)

export const serviceDetails = { gynecology, beauty, slim } as const

export function toSummary({
  contentHtml: _contentHtml,
  seoTitle: _seoTitle,
  metaDescription: _metaDescription,
  updatedAt: _updatedAt,
  ...summary
}: Post): PostSummary {
  return summary
}
