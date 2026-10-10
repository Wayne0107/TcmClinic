<script setup lang="ts">
// 關於我們：文字逐字取自舊站 /about（資料見 server/mock/pages/about.ts）
const [{ data: about }, { data: doctors }] = await Promise.all([useAboutContent(), useDoctors()])
if (!about.value) throw createError({ statusCode: 404, message: '找不到頁面', fatal: true })
const a = about.value
// 舊站順序：…首次到診須知 → 常見問題 → 兩間分店
const mainSections = a.sections.filter((x) => x.id !== 'branches')
const branches = a.sections.find((x) => x.id === 'branches')
const { siteUrl } = useRuntimeConfig().public

useSiteSeo({
  title: a.seo.title,
  description: a.seo.description,
  exact: true,
  path: '/about',
  image: '/images/storefront-sign-wide-1200.webp',
})
useJsonLd(() => [
  { '@type': 'AboutPage', url: `${siteUrl}/about`, name: a.seo.title },
  ...(doctors.value ?? []).map((d) => schema.physician(d, siteUrl)),
])
</script>

<template>
  <div v-if="about">
    <PageHero
      :title="about.banner.title"
      :lead="about.banner.lead"
      :image="{ src: 'storefront-sign-3', alt: '聖儀堂中醫診所招牌' }"
      :breadcrumbs="[{ name: '關於我們' }]"
    />
    <StatsBar :stats="about.stats" />

    <ContentSectionView
      v-for="(sec, i) in mainSections"
      :key="sec.id ?? sec.title"
      :section="sec"
      :index="i"
    />

    <section id="faq" class="section bg-stone" aria-labelledby="faq-title">
      <div class="wrap-narrow">
        <SectionHead
          heading-id="faq-title"
          :en="about.faq.en"
          :title="about.faq.title"
          :lead="about.faq.lead"
        />
        <div class="mt-10"><FaqList :faqs="about.faq.items" /></div>
      </div>
    </section>

    <ContentSectionView v-if="branches" :section="branches" :index="0" />

    <CtaBand :cta="about.cta" />
  </div>
</template>
