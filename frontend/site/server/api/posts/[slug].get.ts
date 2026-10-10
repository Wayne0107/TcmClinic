export default defineEventHandler((event) => {
  const post = posts.find((p) => p.slug === getRouterParam(event, 'slug'))
  if (!post) throw createError({ statusCode: 404, message: '找不到文章' })
  return post
})
