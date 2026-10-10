<script setup lang="ts">
import type { ContentSection } from '#shared/types/api'

// 一個內容區塊：英文小標＋標題＋導言＋內容＋插入元件（醫師、分店、影片、相簿、預約表單）。
// 純文字長內容延後繪製（cv-auto）以減少主執行緒工作；有插入元件（表單、卡片）的區塊正常繪製。
// 背景依 index 輪替（同 prototype/blush.html：白 → 淡粉 → 白 → 暖灰）。
const props = withDefaults(
  defineProps<{ section: ContentSection; index?: number; narrow?: boolean }>(),
  { index: 0, narrow: true },
)
const bg = computed(() => ['', 'bg-blush-bg', '', 'bg-stone'][props.index % 4])
const headingId = computed(() => `${props.section.id ?? `sec-${props.index}`}-title`)
</script>

<template>
  <section
    :id="section.id"
    class="section"
    :class="[bg, section.slot ? '' : 'cv-auto']"
    :aria-labelledby="headingId"
  >
    <div :class="narrow && !section.slot ? 'wrap-narrow' : 'wrap'">
      <SectionHead :heading-id="headingId" :en="section.en" :title="section.title" />
      <RichText
        v-if="section.lead"
        :text="section.lead"
        tag="p"
        class="sec-lead mx-auto text-center"
      />
      <div :class="section.slot ? 'mx-auto max-w-[820px]' : ''">
        <ContentBlocks v-if="section.blocks.length" :blocks="section.blocks" class="mt-10" />
      </div>
      <div v-if="section.slot" class="mt-12">
        <SectionSlotView :slot-name="section.slot" />
      </div>
    </div>
  </section>
</template>
