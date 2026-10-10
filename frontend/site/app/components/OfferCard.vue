<script setup lang="ts">
import type { Offer } from '#shared/types/api'

// 優惠區塊：淺色色塊＋右上角裝飾圓圈（同 prototype/blush.html）；文字為舊站收費一覽原文
const props = withDefaults(
  defineProps<{ offer: Offer; headingLevel?: 'h2' | 'h3'; tone?: number }>(),
  { headingLevel: 'h3', tone: 0 },
)
const bg = computed(() => ['bg-petal', 'bg-stone', 'bg-blush-bg'][props.tone % 3])
</script>

<template>
  <article
    :id="offer.id"
    class="relative flex h-full min-h-56 flex-col justify-end overflow-hidden p-8"
    :class="bg"
  >
    <span
      class="pointer-events-none absolute -top-10 -right-10 size-56 rounded-full border border-blush opacity-60"
      aria-hidden="true"
    />
    <span
      v-if="tone % 3 === 0"
      class="pointer-events-none absolute -top-2 -right-2 size-36 rounded-full bg-blush-bg opacity-70"
      aria-hidden="true"
    />
    <component
      :is="headingLevel"
      class="relative font-sans text-2xl font-bold tracking-[0.1em] text-cocoa"
    >
      {{ offer.title }}
    </component>
    <p class="relative mt-3 font-serif text-3xl font-bold text-blush-ink">{{ offer.price }}</p>
    <p class="relative mt-3 text-[15px] leading-[1.8]">{{ offer.description }}</p>
    <NuxtLink
      :to="{
        path: '/booking',
        query: offer.bookingServiceId ? { service: offer.bookingServiceId } : {},
      }"
      class="relative mt-5 text-sm tracking-[0.08em] text-cocoa underline underline-offset-4 hover:text-rose"
      >立即網上預約 →<span class="sr-only">：{{ offer.title }}</span></NuxtLink
    >
  </article>
</template>
