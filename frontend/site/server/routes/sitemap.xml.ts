import { doctors, posts, serviceTopics } from '../utils/mock-db'

const STATIC_PAGES: [path: string, priority: number, changefreq: string][] = [
  ['/', 1.0, 'weekly'],
  ['/about', 0.8, 'monthly'],
  ['/services', 0.8, 'monthly'],
  ['/gynecology', 0.8, 'monthly'],
  ['/beauty', 0.8, 'monthly'],
  ['/slim', 0.8, 'monthly'],
  ['/general', 0.8, 'monthly'],
  ['/doctors', 0.7, 'monthly'],
  ['/visit', 0.6, 'monthly'],
  ['/pricing', 0.6, 'monthly'],
  ['/safety', 0.5, 'monthly'],
  ['/faq', 0.6, 'monthly'],
  ['/offers', 0.6, 'monthly'],
  ['/contact', 0.8, 'monthly'],
  ['/booking', 0.6, 'monthly'],
  ['/blog', 0.8, 'weekly'],
]

export default defineEventHandler((event) => {
  const base = useRuntimeConfig(event).public.siteUrl
  const latest = posts[0]?.publishedAt
  const urls: { loc: string; priority: number; changefreq: string; lastmod?: string }[] = [
    ...STATIC_PAGES.map(([path, priority, changefreq]) => ({
      loc: path,
      priority,
      changefreq,
      lastmod: path === '/blog' ? latest : undefined,
    })),
    ...Object.values(serviceTopics)
      .flat()
      .map((t) => ({ loc: t.path, priority: 0.7, changefreq: 'monthly' })),
    ...doctors.map((d) => ({ loc: `/doctors/${d.slug}`, priority: 0.6, changefreq: 'monthly' })),
    ...posts.map((p) => ({
      loc: `/blog/${p.slug}`,
      priority: 0.7,
      changefreq: 'monthly',
      lastmod: p.updatedAt,
    })),
  ]

  setHeader(event, 'content-type', 'application/xml; charset=utf-8')
  return `<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
${urls.map((u) => `  <url><loc>${base}${u.loc}</loc>${u.lastmod ? `<lastmod>${u.lastmod}</lastmod>` : ''}<changefreq>${u.changefreq}</changefreq><priority>${u.priority.toFixed(1)}</priority></url>`).join('\n')}
</urlset>`
})
