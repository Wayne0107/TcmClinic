export default defineEventHandler((event) => {
  const slug = getRouterParam(event, 'slug') as keyof typeof serviceDetails
  const detail = serviceDetails[slug]
  if (!detail) throw createError({ statusCode: 404, message: '找不到服務' })
  return detail
})
