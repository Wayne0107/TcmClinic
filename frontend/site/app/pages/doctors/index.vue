<script setup lang="ts">
// 醫師團隊：標題、導言與醫師介紹皆為舊站 /about「醫師團隊」原文
const [{ data: doctors }, { data: about }] = await Promise.all([useDoctors(), useAboutContent()])
const { siteUrl } = useRuntimeConfig().public
const team = computed(() => about.value?.sections.find((s) => s.id === 'team'))

useSiteSeo({
  title: '醫師團隊',
  // 描述由頁面上的原文句子組成（導言＋第一段）
  description: [team.value?.lead, team.value?.blocks.find((b) => b.type === 'p')]
    .map((x) => (typeof x === 'string' ? x : x && 'text' in x ? plainText(x.text) : ''))
    .join(''),
  path: '/doctors',
  image: '/images/doctor-pan-667.webp',
})
useJsonLd(() => doctors.value?.map((d) => schema.physician(d, siteUrl)))
</script>

<template>
  <div>
    <PageHero
      :eyebrow="team?.en"
      title="醫師團隊"
      :lead="team?.lead"
      :breadcrumbs="[{ name: '關於我們', path: '/about' }, { name: '醫師團隊' }]"
    />
    <section class="section">
      <div class="wrap">
        <ContentBlocks v-if="team" :blocks="team.blocks" class="mx-auto mb-14 max-w-[820px]" />
        <div class="mx-auto grid max-w-[900px] gap-14 md:grid-cols-2 md:gap-8">
          <DoctorCard v-for="d in doctors" :key="d.slug" :doctor="d" heading-level="h2" show-bio />
        </div>
      </div>
    </section>
  </div>
</template>
