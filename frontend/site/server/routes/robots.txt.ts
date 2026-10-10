export default defineEventHandler((event) => {
  const base = useRuntimeConfig(event).public.siteUrl
  setHeader(event, 'content-type', 'text/plain; charset=utf-8')
  return `User-agent: *
Allow: /
Disallow: /api/
Disallow: /booking/thanks

Sitemap: ${base}/sitemap.xml
`
})
