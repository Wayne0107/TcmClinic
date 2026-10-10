<script setup lang="ts">
// 醫師團隊：團隊介紹、兩位醫師、學歷與專業資歷、如何核實註冊資格（舊站 /about 原文，見 server/mock/pages/info.ts）
const [{ data: page }, { data: doctors }] = await Promise.all([
  useInfoPage('doctors'),
  useDoctors(),
])
if (!page.value) throw createError({ statusCode: 404, message: '找不到頁面', fatal: true })
const p = page.value
const { siteUrl } = useRuntimeConfig().public

useSiteSeo({
  title: p.seo.title,
  // 描述由頁面上的原文句子組成（導言＋第一段）
  description: [p.seo.description, p.intro?.find((b) => b.type === 'p')]
    .map((x) => (typeof x === 'string' ? x : x && 'text' in x ? plainText(x.text) : ''))
    .join(''),
  path: '/doctors',
  image: '/images/doctor-pan-667.webp',
})
useJsonLd(() => doctors.value?.map((d) => schema.physician(d, siteUrl)))
</script>

<template>
  <div v-if="page">
    <PageHero
      :eyebrow="page.banner.en"
      :title="page.banner.title"
      :lead="page.banner.lead"
      :breadcrumbs="[{ name: '關於我們', path: '/about' }, { name: page.banner.title }]"
    />
    <section class="section">
      <div class="wrap">
        <ContentBlocks
          v-if="page.intro?.length"
          :blocks="page.intro"
          class="mx-auto mb-14 max-w-[820px]"
        />
        <div class="mx-auto grid max-w-[900px] gap-14 md:grid-cols-2 md:gap-8">
          <DoctorCard v-for="d in doctors" :key="d.slug" :doctor="d" heading-level="h2" show-bio />
        </div>
      </div>
    </section>

    <ContentSectionView
      v-for="(sec, i) in page.sections"
      :key="sec.id ?? sec.title"
      :section="sec"
      :index="i + 1"
    />

    <section v-if="page.faq" class="section bg-stone">
      <div class="wrap-narrow"><ArticleFaq :faq="page.faq" align="center" /></div>
    </section>

    <CtaBand v-if="page.cta" :cta="page.cta" />
  </div>
</template>
