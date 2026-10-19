<script setup lang="ts">
import type { ContentSection } from '#shared/types/api'

// 服務頁內文的一個段落（窄欄、左對齊）：英文小標＋H2＋導言＋內容。目錄頁與主題子頁共用。
const props = withDefaults(defineProps<{ section: ContentSection; index?: number }>(), {
  index: 0,
})
const headingId = computed(() => `${props.section.id ?? `sec-${props.index}`}-title`)
</script>

<template>
  <section :id="section.id" :class="{ 'cv-auto': index > 0 }" :aria-labelledby="headingId">
    <p v-if="section.en" class="eyebrow break-words">{{ section.en }}</p>
    <h2
      :id="headingId"
      class="mt-1 font-serif text-[1.6rem] leading-[1.45] font-bold tracking-[0.1em] text-cocoa md:text-[1.9rem]"
    >
      {{ section.title }}
    </h2>
    <span class="mt-4 block h-px w-10 bg-blush" aria-hidden="true" />
    <RichText v-if="section.lead" :text="section.lead" tag="p" class="prose-tcm mt-6 text-muted" />
    <ContentBlocks :blocks="section.blocks" class="mt-6" />
    <slot />
  </section>
</template>
