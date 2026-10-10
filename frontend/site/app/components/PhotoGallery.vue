<script setup lang="ts">
import type { ImageRef } from '#shared/types/api'

// 相片網格 + 燈箱（原生 <dialog>：自動焦點管理、Esc 關閉、背景不可操作）
const props = defineProps<{ photos: ImageRef[] }>()
const dialog = useTemplateRef<HTMLDialogElement>('dialog')
const index = ref(0)
const current = computed(() => props.photos[index.value]!)

function open(i: number) {
  index.value = i
  dialog.value?.showModal()
}
function go(delta: number) {
  index.value = (index.value + delta + props.photos.length) % props.photos.length
}
function onKey(e: KeyboardEvent) {
  if (e.key === 'ArrowLeft') go(-1)
  if (e.key === 'ArrowRight') go(1)
}
function onBackdrop(e: MouseEvent) {
  if (e.target === dialog.value) dialog.value?.close()
}
</script>

<template>
  <div>
    <ul class="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
      <li v-for="(p, i) in photos" :key="p.src">
        <button
          type="button"
          class="group block w-full overflow-hidden"
          :aria-label="`放大相片：${p.alt}`"
          @click="open(i)"
        >
          <ResponsiveImage
            :src="p.src"
            :alt="p.alt"
            sizes="(min-width: 1024px) 280px, (min-width: 640px) 30vw, 45vw"
            class="aspect-square w-full object-cover transition-transform duration-500 group-hover:scale-105"
          />
        </button>
      </li>
    </ul>

    <dialog
      ref="dialog"
      class="m-auto max-h-none max-w-none bg-transparent p-0 backdrop:bg-black/85"
      aria-label="相片瀏覽"
      @keydown="onKey"
      @click="onBackdrop"
    >
      <figure class="flex max-h-[92vh] w-[min(92vw,1100px)] flex-col items-center">
        <ResponsiveImage
          :key="current.src"
          :src="current.src"
          :alt="current.alt"
          sizes="92vw"
          class="max-h-[80vh] w-auto object-contain"
        />
        <figcaption class="mt-3 text-sm text-white">
          {{ current.alt }}（{{ index + 1 }} / {{ photos.length }}）
        </figcaption>
      </figure>
      <button
        type="button"
        class="fixed top-4 right-4 grid size-12 place-items-center rounded-full bg-white/15 text-2xl text-white hover:bg-white/30"
        aria-label="關閉"
        @click="dialog?.close()"
      >
        ×
      </button>
      <button
        type="button"
        class="fixed top-1/2 left-3 grid size-12 -translate-y-1/2 place-items-center rounded-full bg-white/15 text-2xl text-white hover:bg-white/30"
        aria-label="上一張"
        @click="go(-1)"
      >
        ‹
      </button>
      <button
        type="button"
        class="fixed top-1/2 right-3 grid size-12 -translate-y-1/2 place-items-center rounded-full bg-white/15 text-2xl text-white hover:bg-white/30"
        aria-label="下一張"
        @click="go(1)"
      >
        ›
      </button>
    </dialog>
  </div>
</template>
