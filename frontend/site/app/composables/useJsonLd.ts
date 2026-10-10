import type { ClinicInfo, Doctor, Faq } from '#shared/types/api'

/** 輸出 JSON-LD；傳入 getter 以便資料載入後更新 */
export function useJsonLd(
  data: MaybeRefOrGetter<Record<string, unknown> | Record<string, unknown>[] | null | undefined>,
) {
  useHead(() => {
    const value = toValue(data)
    if (!value) return {}
    const graph = Array.isArray(value) ? value : [value]
    return {
      script: [
        {
          type: 'application/ld+json',
          innerHTML: JSON.stringify({ '@context': 'https://schema.org', '@graph': graph }),
        },
      ],
    }
  })
}

const DAY_NAMES = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']

/** 本站 RichText 轉純文字（JSON-LD 不可含 Markdown） */
export function plainText(text: string) {
  return text.replace(/\*\*(.+?)\*\*/g, '$1').replace(/\[(.+?)\]\((.+?)\)/g, '$1')
}

export const schema = {
  clinicId: (siteUrl: string) => `${siteUrl}/#clinic`,

  clinic(c: ClinicInfo, siteUrl: string) {
    const hours = c.hours.map((h) => ({
      '@type': 'OpeningHoursSpecification',
      dayOfWeek: h.days.map((d) => DAY_NAMES[d]),
      opens: h.open,
      closes: h.close,
    }))
    return {
      '@type': ['MedicalClinic', 'LocalBusiness'],
      '@id': `${siteUrl}/#clinic`,
      name: c.name,
      alternateName: c.nameEn,
      description: c.description,
      url: `${siteUrl}/`,
      logo: `${siteUrl}/icon-512.png`,
      image: `${siteUrl}/og-default.jpg`,
      telephone: c.phoneE164,
      priceRange: '$$',
      currenciesAccepted: 'HKD',
      medicalSpecialty: ['Gynecologic', 'Dermatology'],
      // 服務地區同舊站 JSON-LD
      areaServed: ['觀塘', '藍田', '油塘', '九龍灣', '牛頭角'].map((name) => ({
        '@type': 'Place',
        name,
      })),
      openingHoursSpecification: hours,
      department: c.branches.map((b) => ({
        '@type': 'MedicalClinic',
        '@id': `${siteUrl}/contact#${b.slug}`,
        name: `${c.name}（${b.name}）`,
        telephone: c.phoneE164,
        image: `${siteUrl}/images/${b.image.src}-800.webp`,
        address: {
          '@type': 'PostalAddress',
          streetAddress: b.address,
          addressLocality: b.district,
          addressRegion: '九龍',
          addressCountry: 'HK',
        },
        hasMap: mapUrl(b.mapQuery),
        openingHoursSpecification: hours,
      })),
    }
  },

  physician(d: Doctor, siteUrl: string) {
    return {
      '@type': 'Physician',
      '@id': `${siteUrl}/doctors/${d.slug}#physician`,
      name: d.name,
      jobTitle: '註冊中醫',
      description: plainText(d.bio[0] ?? d.headline),
      url: `${siteUrl}/doctors/${d.slug}`,
      image: d.photo ? `${siteUrl}/images/${d.photo.src}-800.webp` : undefined,
      gender: d.gender === 'female' ? 'Female' : 'Male',
      worksFor: { '@id': `${siteUrl}/#clinic` },
    }
  },

  breadcrumbs(items: { name: string; path: string }[], siteUrl: string) {
    return {
      '@type': 'BreadcrumbList',
      itemListElement: items.map((it, i) => ({
        '@type': 'ListItem',
        position: i + 1,
        name: it.name,
        item: `${siteUrl}${it.path}`,
      })),
    }
  },

  faq(faqs: Faq[]) {
    return {
      '@type': 'FAQPage',
      mainEntity: faqs.map((f) => ({
        '@type': 'Question',
        name: f.q,
        acceptedAnswer: { '@type': 'Answer', text: plainText(f.a) },
      })),
    }
  },
}
