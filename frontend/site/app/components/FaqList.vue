<script setup lang="ts">
import type { Faq } from '#shared/types/api'

// FAQ 手風琴（原生 details，鍵盤可用）+ FAQPage JSON-LD
const props = withDefaults(defineProps<{ faqs: Faq[]; withSchema?: boolean }>(), {
  withSchema: true,
})
if (props.withSchema) useJsonLd(() => (props.faqs.length ? schema.faq(props.faqs) : null))
</script>

<template>
  <div class="divide-y divide-line border-y border-line">
    <details v-for="(f, i) in faqs" :key="i" class="group">
      <summary
        class="flex cursor-pointer list-none items-start justify-between gap-6 py-5 [&::-webkit-details-marker]:hidden"
      >
        <h3 class="font-sans text-[16.5px] leading-7 font-semibold tracking-[0.04em] text-cocoa-2">
          {{ f.q }}
        </h3>
        <span
          aria-hidden="true"
          class="mt-1 grid size-6 shrink-0 place-items-center rounded-full border border-blush text-blush-ink transition-transform group-open:rotate-45"
          >+</span
        >
      </summary>
      <RichText :text="f.a" tag="p" class="pr-10 pb-6 text-[15px] leading-8 text-body" />
    </details>
  </div>
</template>
