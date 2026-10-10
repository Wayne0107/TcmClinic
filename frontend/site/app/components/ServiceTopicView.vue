<script setup lang="ts">
import type { ServiceDetail } from '#shared/types/api'

// 服務主題子頁共用版面：/gynecology/menstrual 等。目錄（同科其他主題）取自目錄頁資料。
const props = defineProps<{ service: ServiceDetail['slug']; topic: string }>()

const [{ data: topic }, { data: hub }] = await Promise.all([
  useServiceTopic(props.service, props.topic),
  useServiceDetail(props.service),
])
if (!topic.value || !hub.value)
  throw createError({ statusCode: 404, message: '找不到頁面', fatal: true })
const { siteUrl } = useRuntimeConfig().public

const t = topic.value
// 頁首導言即第一段的導言，第一段不再重複顯示
const sections = t.sections.map((sec, i) =>
  i === 0 && sec.lead === t.lead ? { ...sec, lead: undefined } : sec,
)
useSiteSeo({
  title: t.seo.title,
  description: plainText(t.seo.description),
  path: t.path,
  image: `/images/${hub.value.hero.image.src}-1200.webp`,
  imageAlt: hub.value.hero.image.alt,
})
useJsonLd(() => ({
  '@type': 'MedicalWebPage',
  '@id': `${siteUrl}${t.path}#webpage`,
  url: `${siteUrl}${t.path}`,
  name: t.title,
  description: plainText(t.seo.description),
  inLanguage: 'zh-Hant-HK',
  about: { '@type': 'MedicalTherapy', name: t.title },
  isPartOf: { '@id': `${siteUrl}${t.servicePath}#webpage` },
  provider: { '@id': `${siteUrl}/#clinic` },
}))
</script>

<template>
  <div v-if="topic && hub">
    <PageHero
      :eyebrow="topic.en"
      :title="topic.title"
      :lead="topic.lead"
      :breadcrumbs="[
        { name: '診療服務', path: '/services' },
        { name: topic.serviceName, path: topic.servicePath },
        { name: topic.title },
      ]"
    />

    <ServiceToc
      variant="bar"
      :service-name="hub.name"
      :service-path="hub.path"
      :topics="hub.topics"
    />

    <div class="wrap grid gap-12 py-14 lg:grid-cols-[240px_1fr] lg:gap-16 lg:py-20">
      <ServiceToc
        variant="side"
        :service-name="hub.name"
        :service-path="hub.path"
        :topics="hub.topics"
        :booking-label="topic.cta.label"
        :booking-service-id="topic.bookingServiceId"
      />

      <div class="w-full min-w-0 max-w-[780px] space-y-20">
        <ArticleSection
          v-for="(sec, i) in sections"
          :key="sec.id ?? sec.title"
          :section="sec"
          :index="i"
        />
        <ArticleFaq :faq="topic.faq" />
      </div>
    </div>

    <CtaBand :cta="topic.cta" :booking-query="{ service: topic.bookingServiceId }" />
  </div>
</template>
