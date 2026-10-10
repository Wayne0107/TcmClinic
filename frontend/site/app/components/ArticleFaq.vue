<script setup lang="ts">
import type { FaqSection } from '#shared/types/api'

// 頁面上的精選常見問題（全站 FAQ 的子集）。FAQPage 結構化資料只在 /faq 輸出，這裡不重複。
defineProps<{ faq: FaqSection; align?: 'left' | 'center' }>()
</script>

<template>
  <section v-if="faq.items.length" id="faq" aria-labelledby="faq-title">
    <SectionHead
      heading-id="faq-title"
      :en="faq.en"
      :title="faq.title"
      :lead="faq.lead"
      :align="align ?? 'left'"
    />
    <div class="mt-8"><FaqList :faqs="faq.items" :with-schema="false" /></div>
    <p v-if="faq.note" class="mt-6 text-[13.5px] leading-7 text-muted">{{ faq.note }}</p>
    <p class="mt-6" :class="align === 'center' ? 'text-center' : ''">
      <NuxtLink to="/faq" class="text-link">查看全部常見問題 →</NuxtLink>
    </p>
  </section>
</template>
