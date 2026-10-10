<script setup lang="ts">
import type { ServiceDetail } from '#shared/types/api'

// 服務目錄頁共用版面：/gynecology、/beauty、/slim、/general。
// 簡介 → 主題卡片（連到各子頁）→ 概覽段落 → 精選常見問題；深入內容在子頁（ServiceTopicView）。
const props = defineProps<{ slug: ServiceDetail['slug'] }>()

const { data: service } = await useServiceDetail(props.slug)
if (!service.value) throw createError({ statusCode: 404, message: '找不到服務', fatal: true })
const { siteUrl } = useRuntimeConfig().public

const s = service.value
useSiteSeo({
  title: s.seo.title,
  description: s.seo.description,
  exact: s.slug !== 'general',
  path: s.path,
  image: `/images/${s.hero.image.src}-1200.webp`,
  imageAlt: s.hero.image.alt,
})
useJsonLd(() => ({
  '@type': 'MedicalWebPage',
  '@id': `${siteUrl}${s.path}#webpage`,
  url: `${siteUrl}${s.path}`,
  name: s.seo.title,
  description: s.seo.description,
  inLanguage: 'zh-Hant-HK',
  about: { '@type': 'MedicalTherapy', name: s.name },
  hasPart: s.topics.map((t) => ({ '@type': 'MedicalWebPage', url: `${siteUrl}${t.path}` })),
  provider: { '@id': `${siteUrl}/#clinic` },
}))

// 麵包屑最後一層文字同舊站
const crumb = {
  gynecology: '中醫婦科',
  beauty: '中醫美容・面部經絡理療',
  slim: '修身塑形',
  general: s.name,
}[s.slug]
const [intro, ...rest] = s.sections
const hasToc = s.topics.length > 0
</script>

<template>
  <div v-if="service">
    <PageHero
      :eyebrow="service.hero.en"
      :title="service.hero.title"
      :subtitle="service.hero.subtitle"
      :lead="service.hero.lead"
      :image="service.hero.image"
      :breadcrumbs="[{ name: '診療服務', path: '/services' }, { name: crumb }]"
    >
      <ContentBlocks
        v-if="service.prices.length"
        :blocks="[{ type: 'prices', items: service.prices }]"
        class="mt-7"
      />
    </PageHero>

    <ServiceToc
      v-if="hasToc"
      variant="bar"
      :service-name="service.name"
      :service-path="service.path"
      :topics="service.topics"
    />

    <div
      class="wrap grid gap-12 py-14 lg:py-20"
      :class="hasToc ? 'lg:grid-cols-[240px_1fr] lg:gap-16' : ''"
    >
      <ServiceToc
        v-if="hasToc"
        variant="side"
        :service-name="service.name"
        :service-path="service.path"
        :topics="service.topics"
        :booking-label="service.cta.label"
        :booking-service-id="service.bookingServiceId"
      />

      <div class="w-full min-w-0 max-w-[780px] space-y-20" :class="hasToc ? '' : 'mx-auto'">
        <ArticleSection v-if="intro" :section="intro" :index="0" />

        <section v-if="hasToc" id="topics" aria-labelledby="topics-title">
          <p class="eyebrow">TOPICS</p>
          <h2
            id="topics-title"
            class="mt-1 font-serif text-[1.6rem] leading-[1.45] font-bold tracking-[0.1em] text-cocoa md:text-[1.9rem]"
          >
            診療主題
          </h2>
          <span class="mt-4 block h-px w-10 bg-blush" aria-hidden="true" />
          <RichText
            v-if="service.topicsLead"
            :text="service.topicsLead"
            tag="p"
            class="prose-tcm mt-6 text-muted"
          />
          <TopicCards :topics="service.topics" class="mt-8" />
        </section>

        <ArticleSection
          v-for="(sec, i) in rest"
          :key="sec.id ?? sec.title"
          :section="sec"
          :index="i + 1"
        />

        <ArticleFaq :faq="service.faq" />
      </div>
    </div>

    <CtaBand :cta="service.cta" :booking-query="{ service: service.bookingServiceId }" />
  </div>
</template>
