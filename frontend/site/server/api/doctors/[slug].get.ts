import { doctors } from '../../mock/doctors'

export default defineEventHandler((event) => {
  const doctor = doctors.find((d) => d.slug === getRouterParam(event, 'slug'))
  if (!doctor) throw createError({ statusCode: 404, message: '找不到醫師' })
  return doctor
})
