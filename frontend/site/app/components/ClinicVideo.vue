<script setup lang="ts">
// 診所環境影片（舊站同一支影片；不支援時的提示文字同舊站）
defineProps<{ fallback?: string }>()
</script>

<template>
  <div class="mx-auto max-w-3xl">
    <video
      controls
      preload="none"
      poster="/images/reception-800.webp"
      class="aspect-video w-full bg-slate object-cover"
      title="聖儀堂中醫診所觀塘診所環境影片"
    >
      <source src="/media/clinic-tour.mp4" type="video/mp4" />
      {{ fallback ?? '您的瀏覽器不支援影片播放。' }}
    </video>
  </div>
</template>
