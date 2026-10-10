import type { Paged, PostSummary } from '#shared/types/api'
import { posts, toSummary } from '../../utils/mock-db'

export default defineEventHandler((event): Paged<PostSummary> => {
  const query = getQuery(event)
  const pageSize = Math.min(Number(query.pageSize) || 15, 100)
  const exclude = typeof query.exclude === 'string' ? query.exclude : undefined

  const filtered = posts.filter((p) => p.slug !== exclude)
  const totalPages = Math.max(1, Math.ceil(filtered.length / pageSize))
  const page = Number(query.page) || 1
  if (page < 1 || page > totalPages) throw createError({ statusCode: 404, message: '頁數不存在' })

  return {
    items: filtered.slice((page - 1) * pageSize, page * pageSize).map(toSummary),
    page,
    pageSize,
    totalCount: filtered.length,
  }
})
