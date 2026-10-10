<script setup lang="ts">
import type { ImageRef } from '#shared/types/api'

// 內頁頁首：麵包屑 + 英文小標 + H1 + 導言；有圖時右側拱形相片
const props = defineProps<{
  eyebrow?: string
  title: string
  /** 未提供時以 title 中的「｜」拆出 */
  subtitle?: string
  lead?: string | string[]
  breadcrumbs: { name: string; path?: string }[]
  image?: ImageRef
}>()

// 以「｜」分成主標與副標：長標題不會在詞中斷行，副標仍在 H1 內（保留關鍵字）
const mainTitle = computed(() => (props.subtitle ? props.title : props.title.split('｜')[0]))
const subTitle = computed(() => props.subtitle ?? props.title.split('｜').slice(1).join('・'))
const leads = computed(() =>
  Array.isArray(props.lead) ? props.lead : props.lead ? [props.lead] : [],
)
</script>

<template>
  <!-- 平面淡粉底（同 prototype/blush.html 主視覺） -->
  <section class="bg-blush-bg">
    <div
      class="wrap grid items-start gap-10 py-10 md:py-16"
      :class="image ? 'md:grid-cols-[1.2fr_1fr]' : ''"
    >
      <div>
        <SiteBreadcrumbs :items="breadcrumbs" />
        <p v-if="eyebrow" class="eyebrow mt-8">{{ eyebrow }}</p>
        <h1
          :class="eyebrow ? 'mt-2' : 'mt-8'"
          class="font-serif text-[2rem] leading-[1.35] font-bold tracking-[0.12em] text-cocoa md:text-[2.75rem]"
        >
          {{ mainTitle }}
          <span
            v-if="subTitle"
            class="mt-2 block text-[1.15rem] leading-[1.6] font-semibold tracking-[0.1em] text-cocoa-2 md:text-[1.4rem]"
            >{{ subTitle }}</span
          >
        </h1>
        <span class="mt-5 block h-px w-12 bg-blush" aria-hidden="true" />
        <RichText
          v-for="l in leads"
          :key="l"
          :text="l"
          tag="p"
          class="mt-6 max-w-2xl text-[15.5px] leading-8 text-body md:text-[17px]"
        />
        <slot />
      </div>
      <div v-if="image" class="mx-auto hidden w-full max-w-[380px] md:block">
        <ResponsiveImage
          :src="image.src"
          :alt="image.alt"
          sizes="(min-width: 768px) 380px, 90vw"
          priority
          preload-media="(min-width: 768px)"
          class="arch aspect-[4/5] w-full object-cover shadow-[var(--shadow-soft)]"
        />
      </div>
    </div>
  </section>
</template>
