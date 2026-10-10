<script setup lang="ts">
import type { ServiceTopicLink } from '#shared/types/api'

// 服務目錄（總覽＋各主題子頁）。目錄頁與主題子頁共用：
// variant="bar"：手機版，頁首下方可橫向捲動的標籤列；variant="side"：桌面版側欄＋預約按鈕。
const props = defineProps<{
  variant: 'bar' | 'side'
  serviceName: string
  servicePath: string
  topics: ServiceTopicLink[]
  bookingLabel?: string
  bookingServiceId?: number
}>()
const route = useRoute()
const links = computed(() => [
  { path: props.servicePath, title: `${props.serviceName}總覽` },
  ...props.topics,
])
const isCurrent = (path: string) => route.path === path
</script>

<template>
  <nav
    v-if="variant === 'bar'"
    :aria-label="`${serviceName}目錄`"
    class="sticky top-[68px] z-20 border-b border-line bg-white/95 backdrop-blur lg:hidden"
  >
    <ul class="wrap flex gap-2 overflow-x-auto py-3 [scrollbar-width:none]">
      <li v-for="l in links" :key="l.path" class="shrink-0">
        <NuxtLink
          :to="l.path"
          class="block rounded-full px-4 py-1.5 text-[13.5px] whitespace-nowrap"
          :class="isCurrent(l.path) ? 'bg-cocoa text-white' : 'bg-blush-bg text-cocoa-2'"
          :aria-current="isCurrent(l.path) ? 'page' : undefined"
          >{{ l.title }}</NuxtLink
        >
      </li>
    </ul>
  </nav>

  <aside v-else class="hidden lg:block">
    <nav :aria-label="`${serviceName}目錄`" class="sticky top-32">
      <p class="eyebrow">CONTENTS</p>
      <ul class="mt-3 space-y-1 border-l border-line">
        <li v-for="l in links" :key="l.path">
          <NuxtLink
            :to="l.path"
            class="-ml-px block border-l-2 py-1.5 pl-4 text-[14px] transition-colors"
            :class="
              isCurrent(l.path)
                ? 'border-cocoa font-medium text-cocoa'
                : 'border-transparent text-muted hover:text-cocoa'
            "
            :aria-current="isCurrent(l.path) ? 'page' : undefined"
            >{{ l.title }}</NuxtLink
          >
        </li>
      </ul>
      <NuxtLink
        v-if="bookingLabel"
        :to="{ path: '/booking', query: bookingServiceId ? { service: bookingServiceId } : {} }"
        class="btn-primary mt-8 w-full"
        >{{ bookingLabel }}</NuxtLink
      >
    </nav>
  </aside>
</template>
