export default defineEventHandler((event) => {
  const slug = getRouterParam(event, 'slug') ?? ''
  // hasOwn：避免 constructor、__proto__ 等原型屬性被當成服務
  if (!Object.hasOwn(serviceDetails, slug))
    throw createError({ statusCode: 404, message: '找不到服務' })
  return serviceDetails[slug as keyof typeof serviceDetails]
})
