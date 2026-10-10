<script setup lang="ts">
// 常見問題：全站 FAQ 單一來源，依分類列出（各頁只引用其中幾題）。FAQPage 結構化資料只在此頁輸出。
const { data: page } = await useFaqPage()
if (!page.value) throw createError({ statusCode: 500, message: '常見問題載入失敗', fatal: true })
const p = page.value

useSiteSeo({ title: p.seo.title, description: p.seo.description, path: '/faq' })
useJsonLd(() => schema.faq(p.categories.flatMap((c) => c.items)))
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

    <div class="wrap-narrow py-14 lg:py-20">
      <nav aria-label="問題分類">
        <ul class="flex flex-wrap gap-2">
          <li v-for="c in page.categories" :key="c.slug">
            <a
              :href="`#${c.slug}`"
              class="inline-block rounded-full border border-line px-4 py-1.5 text-[14px] text-cocoa-2 hover:border-blush hover:bg-blush-bg"
              >{{ c.name }}（{{ c.items.length }}）</a
            >
          </li>
        </ul>
      </nav>

      <section
        v-for="c in page.categories"
        :id="c.slug"
        :key="c.slug"
        class="mt-16 scroll-mt-32"
        :aria-labelledby="`${c.slug}-title`"
      >
        <h2
          :id="`${c.slug}-title`"
          class="font-serif text-[1.5rem] font-bold tracking-[0.1em] text-cocoa md:text-[1.75rem]"
        >
          {{ c.name }}
        </h2>
        <span class="mt-3 block h-px w-10 bg-blush" aria-hidden="true" />
        <div class="mt-6"><FaqList :faqs="c.items" :with-schema="false" /></div>
      </section>

      <p v-if="page.note" class="mt-12 text-[13.5px] leading-7 text-muted">{{ page.note }}</p>
    </div>
  </div>
</template>
