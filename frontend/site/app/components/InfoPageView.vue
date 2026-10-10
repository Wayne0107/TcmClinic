<script setup lang="ts">
// 「就診須知」各頁共用版面：/visit、/pricing、/safety（文字為舊站原文，見 server/mock/pages/info.ts）
const props = defineProps<{ slug: 'visit' | 'pricing' | 'safety' }>()

const { data: page } = await useInfoPage(props.slug)
if (!page.value) throw createError({ statusCode: 404, message: '找不到頁面', fatal: true })
const p = page.value
const route = useRoute()

useSiteSeo({ title: p.seo.title, description: plainText(p.seo.description), path: route.path })
</script>

<template>
  <div v-if="page">
    <PageHero
      :eyebrow="page.banner.en"
      :title="page.banner.title"
      :lead="page.banner.lead"
      :breadcrumbs="[{ name: '就診須知', path: '/visit' }, { name: page.banner.title }]"
    />
    <VisitTabs />

    <div class="wrap-narrow space-y-20 py-14 lg:py-20">
      <ContentBlocks v-if="page.intro?.length" :blocks="page.intro" />
      <ArticleSection
        v-for="(sec, i) in page.sections"
        :key="sec.id ?? sec.title"
        :section="sec"
        :index="i"
      />
      <ArticleFaq v-if="page.faq" :faq="page.faq" />
    </div>

    <CtaBand v-if="page.cta" :cta="page.cta" />
  </div>
</template>
