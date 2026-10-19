<script setup lang="ts">
// 診療服務一覽（目錄）：文字逐字取自舊站 /services（資料見 server/mock/services/overview.ts）
// 開頭介紹 → 四科卡片（連到各科目錄頁）→ 療程選擇對照表 → 精選常見問題
const { data: overview } = await useServicesOverview()
if (!overview.value) throw createError({ statusCode: 404, message: '找不到頁面', fatal: true })
const o = overview.value
const [intro, ...rest] = o.sections

useSiteSeo({ title: o.seo.title, description: o.seo.description, exact: true, path: '/services' })
</script>

<template>
  <div v-if="overview">
    <PageHero
      :title="overview.banner.title"
      :lead="overview.banner.lead"
      :breadcrumbs="[{ name: '診療服務' }]"
    />

    <ContentSectionView v-if="intro" :section="intro" :index="0" />

    <section class="section pt-0 md:pt-0" aria-label="服務項目">
      <div class="wrap">
        <ul class="grid gap-6 md:grid-cols-2">
          <li v-for="s in overview.services" :key="s.slug">
            <NuxtLink
              :to="s.path"
              class="group grid h-full overflow-hidden border border-line bg-paper transition-colors hover:border-blush sm:grid-cols-[200px_1fr]"
            >
              <ResponsiveImage
                :src="s.image.src"
                :alt="s.image.alt"
                sizes="(min-width: 640px) 200px, 92vw"
                class="aspect-[16/9] h-full w-full object-cover sm:aspect-auto"
              />
              <div class="flex flex-col p-6">
                <h2
                  class="font-sans text-xl font-bold tracking-[0.1em] text-cocoa group-hover:text-rose"
                >
                  {{ s.name }}
                </h2>
                <p class="mt-3 flex-1 text-[14.5px] leading-7 text-muted">{{ s.summary }}</p>
                <ul class="mt-4 flex flex-wrap gap-1.5" aria-label="主要項目">
                  <li v-for="tag in s.tags" :key="tag" class="tag">{{ tag }}</li>
                </ul>
                <span class="mt-4 text-sm font-medium tracking-[0.06em] text-cocoa"
                  >了解更多 →</span
                >
              </div>
            </NuxtLink>
          </li>
        </ul>
      </div>
    </section>

    <ContentSectionView
      v-for="(sec, i) in rest"
      :key="sec.id ?? sec.title"
      :section="sec"
      :index="i + 1"
    />

    <section class="section bg-stone">
      <div class="wrap-narrow"><ArticleFaq :faq="overview.faq" align="center" /></div>
    </section>

    <CtaBand :cta="overview.cta" />
  </div>
</template>
