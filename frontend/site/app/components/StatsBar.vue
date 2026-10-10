<script setup lang="ts">
import type { StatItem } from '#shared/types/api'

// 數字列（舊站的統計列；數字與說明為舊站原文）
defineProps<{ stats: StatItem[] }>()
</script>

<template>
  <section aria-label="診所概況" class="border-b border-line bg-paper">
    <dl class="wrap grid grid-cols-2 gap-y-8 py-10 text-center md:grid-cols-4">
      <div v-for="s in stats" :key="s.label" class="flex flex-col items-center px-2">
        <dt class="order-2 mt-2 text-sm tracking-[0.08em] text-muted">{{ s.label }}</dt>
        <dd class="order-1 font-serif text-4xl font-semibold text-rose tabular-nums">
          {{ s.value }}
        </dd>
      </div>
    </dl>
  </section>
</template>
