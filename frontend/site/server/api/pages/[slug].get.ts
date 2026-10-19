import { infoPages } from '../../mock/pages/info'

export default defineEventHandler((event) => {
  const slug = getRouterParam(event, 'slug') ?? ''
  if (!Object.hasOwn(infoPages, slug)) throw createError({ statusCode: 404, message: '找不到頁面' })
  return infoPages[slug]
})
