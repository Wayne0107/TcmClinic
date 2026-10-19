<script setup lang="ts">
import type { ServiceTopicLink } from '#shared/types/api'

// 目錄頁的主題卡片：標題＋該主題第一段的導言，點進子頁
defineProps<{ topics: ServiceTopicLink[]; headingLevel?: 'h2' | 'h3' }>()
</script>

<template>
  <ul class="grid gap-4 sm:grid-cols-2">
    <li v-for="t in topics" :key="t.path">
      <NuxtLink
        :to="t.path"
        class="group flex h-full flex-col border border-line bg-paper p-6 transition-colors hover:border-blush hover:bg-blush-bg"
      >
        <component
          :is="headingLevel ?? 'h3'"
          class="font-sans text-lg font-bold tracking-[0.08em] text-cocoa group-hover:text-rose"
        >
          {{ t.title }}
        </component>
        <RichText
          :text="t.summary"
          tag="p"
          class="mt-2 flex-1 text-[14.5px] leading-7 text-muted"
        />
        <span class="mt-4 text-sm font-medium tracking-[0.06em] text-cocoa">了解更多 →</span>
      </NuxtLink>
    </li>
  </ul>
</template>
