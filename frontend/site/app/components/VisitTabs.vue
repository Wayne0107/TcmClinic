<script setup lang="ts">
// 「就診須知」分頁列（首次就診流程／收費一覽／安全與禁忌／常見問題）
const route = useRoute()
</script>

<template>
  <nav aria-label="就診須知" class="border-b border-line bg-white">
    <ul class="wrap flex gap-2 overflow-x-auto py-3 [scrollbar-width:none]">
      <li v-for="l in visitLinks" :key="l.to" class="shrink-0">
        <NuxtLink
          :to="l.to"
          class="block rounded-full px-4 py-1.5 text-[14px] whitespace-nowrap"
          :class="route.path === l.to ? 'bg-cocoa text-white' : 'bg-blush-bg text-cocoa-2'"
          :aria-current="route.path === l.to ? 'page' : undefined"
          >{{ l.label }}</NuxtLink
        >
      </li>
    </ul>
  </nav>
</template>
