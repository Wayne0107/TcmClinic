<script setup lang="ts">
import type { NuxtError } from '#app'

// 404 文字同舊站；其他錯誤沿用骨架文字，不顯示錯誤細節（docs/FRONTEND.md §5.3）
const props = defineProps<{ error: NuxtError }>()
const isNotFound = computed(() => props.error.statusCode === 404)

useHead({
  title: isNotFound.value ? '找不到頁面｜聖儀堂中醫診所' : '系統發生錯誤｜聖儀堂中醫診所',
})
useSeoMeta({ robots: 'noindex,follow' })

function backToHome() {
  clearError({ redirect: '/' })
}
</script>

<template>
  <NuxtLayout>
    <section class="wrap-narrow flex flex-col items-center py-24 text-center md:py-32">
      <p v-if="isNotFound" class="font-latin text-7xl text-rose">404</p>
      <h1 class="sec-title mt-4">{{ isNotFound ? '找不到頁面' : '系統發生錯誤' }}</h1>
      <span class="sec-rule" aria-hidden="true" />
      <p v-if="isNotFound" class="sec-lead">你所尋找的頁面可能已移動或不存在。</p>
      <p v-else class="sec-lead">請稍後再試；若問題持續發生，請與診所聯絡。</p>
      <button type="button" class="btn-primary mt-8" @click="backToHome">返回首頁</button>
    </section>
  </NuxtLayout>
</template>
