<script setup lang="ts">
// 讀取 shared/image-manifest.json，輸出 WebP srcset 與固有尺寸（避免 CLS）。
import manifest from '#shared/image-manifest.json'

const props = withDefaults(
  defineProps<{
    src: string
    alt: string
    sizes?: string
    /** 首屏主圖：立即載入並提高優先度（LCP） */
    priority?: boolean
    /** 預載只在符合此媒體查詢時發生（例：圖片在手機版隱藏時，避免白白下載） */
    preloadMedia?: string
  }>(),
  { sizes: '100vw', priority: false, preloadMedia: undefined },
)

type Entry = { widths: number[]; width: number; height: number }
const entry = computed(() => (manifest as Record<string, Entry>)[props.src])
const srcset = computed(() =>
  entry.value?.widths.map((w) => `/images/${props.src}-${w}.webp ${w}w`).join(', '),
)
const fallback = computed(() => {
  const widths = entry.value?.widths ?? [800]
  return `/images/${props.src}-${widths.find((w) => w >= 800) ?? widths.at(-1)}.webp`
})

if (props.priority) {
  useHead({
    link: [
      {
        rel: 'preload',
        as: 'image',
        imagesrcset: srcset.value,
        imagesizes: props.sizes,
        fetchpriority: 'high',
        media: props.preloadMedia,
      },
    ],
  })
}
</script>

<template>
  <img
    :src="fallback"
    :srcset="srcset"
    :sizes="sizes"
    :alt="alt"
    :width="entry?.width"
    :height="entry?.height"
    :loading="priority ? 'eager' : 'lazy'"
    :fetchpriority="priority ? 'high' : undefined"
    decoding="async"
  />
</template>
