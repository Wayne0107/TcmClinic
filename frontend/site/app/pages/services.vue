<script setup lang="ts">
// 診療服務一覽：文字逐字取自舊站 /services（資料見 server/mock/services/overview.ts）
const { data: overview } = await useServicesOverview()
if (!overview.value) throw createError({ statusCode: 404, message: '找不到頁面', fatal: true })
const o = overview.value

useSiteSeo({ title: o.seo.title, description: o.seo.description, exact: true, path: '/services' })
</script>

<template>
  <div v-if="overview">
    <PageHero
      :title="overview.banner.title"
      :lead="overview.banner.lead"
      :breadcrumbs="[{ name: '診療服務' }]"
    >
      <nav aria-label="頁內導航" class="mt-8">
        <ul class="flex flex-wrap gap-2">
          <li v-for="n in overview.pageNav" :key="n.id">
            <a
              :href="`#${n.id}`"
              class="inline-block rounded-full bg-white px-4 py-2 text-[13.5px] text-cocoa-2 hover:bg-cocoa hover:text-white"
              >{{ n.label }}</a
            >
          </li>
        </ul>
      </nav>
    </PageHero>

    <ContentSectionView
      v-for="(sec, i) in overview.sections"
      :key="sec.id ?? sec.title"
      :section="sec"
      :index="i"
    />

    <section id="faq" class="section bg-blush-bg" aria-labelledby="faq-title">
      <div class="wrap-narrow">
        <SectionHead
          heading-id="faq-title"
          :en="overview.faq.en"
          :title="overview.faq.title"
          :lead="overview.faq.lead"
        />
        <div class="mt-10"><FaqList :faqs="overview.faq.items" /></div>
        <ContentBlocks
          v-if="overview.keywordLinks.length"
          :blocks="[{ type: 'links', items: overview.keywordLinks }]"
          class="mt-12"
        />
      </div>
    </section>

    <CtaBand :cta="overview.cta" />
  </div>
</template>
