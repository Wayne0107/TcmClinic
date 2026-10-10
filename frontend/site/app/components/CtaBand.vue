<script setup lang="ts">
import type { CtaContent } from '#shared/types/api'

// 頁尾預約區塊：標題、說明、按鈕文字皆由各頁提供舊站原文
const props = defineProps<{ cta: CtaContent; bookingQuery?: Record<string, string | number> }>()
</script>

<template>
  <section class="bg-blush-bg" aria-labelledby="cta-title">
    <div class="wrap flex flex-col items-center py-16 text-center md:py-20">
      <h2 id="cta-title" class="sec-title">{{ cta.title }}</h2>
      <span class="sec-rule" aria-hidden="true" />
      <p v-if="cta.text" class="sec-lead">{{ cta.text }}</p>
      <div class="mt-8">
        <NuxtLink :to="{ path: '/booking', query: props.bookingQuery }" class="btn-primary">{{
          cta.label
        }}</NuxtLink>
      </div>
    </div>
  </section>
</template>
