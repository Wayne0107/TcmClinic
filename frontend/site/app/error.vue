<script setup lang="ts">
import type { NuxtError } from '#app'

const props = defineProps<{ error: NuxtError }>()

// 404 與其他錯誤分開提示；不顯示錯誤細節，避免洩漏內部資訊（docs/FRONTEND.md §5.3）。
const isNotFound = computed(() => props.error.statusCode === 404)

useSeoMeta({
  title: () => (isNotFound.value ? '找不到頁面' : '系統發生錯誤'),
  robots: 'noindex',
})

function backToHome() {
  clearError({ redirect: '/' })
}
</script>

<template>
  <main>
    <h1>{{ isNotFound ? '找不到頁面' : '系統發生錯誤' }}</h1>
    <p v-if="isNotFound">您要找的頁面不存在，請確認網址是否正確。</p>
    <p v-else>請稍後再試；若問題持續發生，請與診所聯絡。</p>
    <button type="button" @click="backToHome">回首頁</button>
  </main>
</template>
