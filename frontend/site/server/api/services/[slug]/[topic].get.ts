export default defineEventHandler((event) => {
  const slug = getRouterParam(event, 'slug') ?? ''
  const topics = Object.hasOwn(serviceTopics, slug)
    ? serviceTopics[slug as keyof typeof serviceTopics]
    : []
  const topic = topics.find((t) => t.slug === getRouterParam(event, 'topic'))
  if (!topic) throw createError({ statusCode: 404, message: '找不到頁面' })
  return topic
})
