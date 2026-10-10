interface SeoInput {
  title: string
  /** true：title 為舊站完整原文，不再加上診所名稱 */
  exact?: boolean
  description: string
  /** 站內路徑，用於 canonical 與 og:url；預設為目前路徑（不含查詢字串） */
  path?: string
  image?: string
  imageAlt?: string
  type?: 'website' | 'article' | 'profile'
  noindex?: boolean
  publishedTime?: string
  modifiedTime?: string
}

const SITE_NAME = '聖儀堂中醫診所'

/** 每頁統一設定 title、description、canonical、Open Graph、Twitter Card */
export function useSiteSeo(input: MaybeRefOrGetter<SeoInput>) {
  const route = useRoute()
  const { siteUrl } = useRuntimeConfig().public

  const seo = computed(() => {
    const v = toValue(input)
    const path = v.path ?? route.path
    const fullTitle = v.exact || v.title.includes(SITE_NAME) ? v.title : `${v.title}｜${SITE_NAME}`
    const image = v.image
      ? v.image.startsWith('http')
        ? v.image
        : `${siteUrl}${v.image}`
      : `${siteUrl}/og-default.jpg`
    return {
      ...v,
      fullTitle,
      url: `${siteUrl}${path === '/' ? '/' : path.replace(/\/$/, '')}`,
      image,
    }
  })

  useHead(() => ({
    title: seo.value.fullTitle,
    link: [{ rel: 'canonical', href: seo.value.url }],
  }))

  useSeoMeta({
    description: () => seo.value.description,
    robots: () => (seo.value.noindex ? 'noindex,follow' : 'index,follow,max-image-preview:large'),
    ogType: () => seo.value.type ?? 'website',
    ogSiteName: SITE_NAME,
    ogLocale: 'zh_HK',
    ogTitle: () => seo.value.fullTitle,
    ogDescription: () => seo.value.description,
    ogUrl: () => seo.value.url,
    ogImage: () => seo.value.image,
    ogImageAlt: () => seo.value.imageAlt ?? seo.value.title,
    twitterCard: 'summary_large_image',
    articlePublishedTime: () => seo.value.publishedTime,
    articleModifiedTime: () => seo.value.modifiedTime,
  })
}
