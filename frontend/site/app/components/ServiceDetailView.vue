<script setup lang="ts">
import type { ServiceDetail } from '#shared/types/api'

// 服務詳情頁共用版面：/gynecology、/beauty、/slim。所有文字由資料提供（舊站原文）。
const props = defineProps<{ slug: ServiceDetail['slug'] }>()

const { data: service } = await useServiceDetail(props.slug)
if (!service.value) throw createError({ statusCode: 404, message: '找不到服務', fatal: true })
const { siteUrl } = useRuntimeConfig().public

const s = service.value
useSiteSeo({
  title: s.seo.title,
  description: s.seo.description,
  exact: true,
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
  provider: { '@id': `${siteUrl}/#clinic` },
}))

// 麵包屑最後一層文字同舊站
const crumb = { gynecology: '中醫婦科', beauty: '中醫美容・面部經絡理療', slim: '修身塑形' }[s.slug]

// 頁內導航高亮（scroll spy）
const activeId = ref(s.pageNav[0]?.id ?? '')
let io: IntersectionObserver | undefined
onMounted(() => {
  io = new IntersectionObserver(
    (entries) => {
      const visible = entries
        .filter((e) => e.isIntersecting)
        .sort((a, b) => a.boundingClientRect.top - b.boundingClientRect.top)
      if (visible[0]) activeId.value = visible[0].target.id
    },
    { rootMargin: '-20% 0px -70% 0px' },
  )
  s.pageNav.forEach((n) => {
    const el = document.getElementById(n.id)
    if (el) io?.observe(el)
  })
})
onBeforeUnmount(() => io?.disconnect())
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

    <!-- 手機版頁內導航 -->
    <nav
      aria-label="頁內導航"
      class="sticky top-[68px] z-20 border-b border-line bg-white/95 backdrop-blur lg:hidden"
    >
      <ul class="wrap flex gap-2 overflow-x-auto py-3 [scrollbar-width:none]">
        <li v-for="n in service.pageNav" :key="n.id" class="shrink-0">
          <a
            :href="`#${n.id}`"
            class="block rounded-full px-4 py-1.5 text-[13.5px] whitespace-nowrap"
            :class="activeId === n.id ? 'bg-cocoa text-white' : 'bg-blush-bg text-cocoa-2'"
            >{{ n.label }}</a
          >
        </li>
      </ul>
    </nav>

    <div class="wrap grid gap-12 py-14 lg:grid-cols-[240px_1fr] lg:gap-16 lg:py-20">
      <!-- 桌面版頁內導航 -->
      <aside class="hidden lg:block">
        <nav aria-label="頁內導航" class="sticky top-32">
          <ul class="space-y-1 border-l border-line">
            <li v-for="n in service.pageNav" :key="n.id">
              <a
                :href="`#${n.id}`"
                class="-ml-px block border-l-2 py-1.5 pl-4 text-[14px] transition-colors"
                :class="
                  activeId === n.id
                    ? 'border-cocoa font-medium text-cocoa'
                    : 'border-transparent text-muted hover:text-cocoa'
                "
                >{{ n.label }}</a
              >
            </li>
          </ul>
          <NuxtLink
            :to="{ path: '/booking', query: { service: service.bookingServiceId } }"
            class="btn-primary mt-8 w-full"
            >{{ service.cta.label }}</NuxtLink
          >
        </nav>
      </aside>

      <div class="max-w-[780px] space-y-20">
        <section
          v-for="(sec, index) in service.sections"
          :id="sec.id"
          :key="sec.id ?? `intro-${index}`"
          :class="{ 'cv-auto': index > 0 }"
          :aria-labelledby="`${sec.id ?? `intro-${index}`}-title`"
        >
          <p v-if="sec.en" class="eyebrow">{{ sec.en }}</p>
          <h2
            :id="`${sec.id ?? `intro-${index}`}-title`"
            class="mt-1 font-serif text-[1.6rem] leading-[1.45] font-bold tracking-[0.1em] text-cocoa md:text-[1.9rem]"
          >
            {{ sec.title }}
          </h2>
          <span class="mt-4 block h-px w-10 bg-blush" aria-hidden="true" />
          <RichText v-if="sec.lead" :text="sec.lead" tag="p" class="prose-tcm mt-6 text-muted" />
          <ContentBlocks :blocks="sec.blocks" class="mt-6" />
        </section>

        <section id="faq" aria-labelledby="faq-title">
          <p v-if="service.faq.en" class="eyebrow">{{ service.faq.en }}</p>
          <h2
            id="faq-title"
            class="mt-1 font-serif text-[1.6rem] font-bold tracking-[0.1em] text-cocoa md:text-[1.9rem]"
          >
            {{ service.faq.title }}
          </h2>
          <span class="mt-4 block h-px w-10 bg-blush" aria-hidden="true" />
          <p v-if="service.faq.lead" class="prose-tcm mt-6 text-muted">{{ service.faq.lead }}</p>
          <div class="mt-6"><FaqList :faqs="service.faq.items" /></div>
          <p v-if="service.faq.note" class="mt-6 text-[13.5px] leading-7 text-muted">
            {{ service.faq.note }}
          </p>
        </section>

        <ContentBlocks
          v-if="service.keywordLinks.length"
          :blocks="[{ type: 'links', items: service.keywordLinks }]"
        />
      </div>
    </div>

    <CtaBand :cta="service.cta" :booking-query="{ service: service.bookingServiceId }" />
  </div>
</template>
