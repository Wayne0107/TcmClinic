<script setup lang="ts">
defineProps<{
  en?: string
  title: string
  lead?: string
  as?: 'h1' | 'h2'
  align?: 'center' | 'left'
  headingId?: string
}>()
</script>

<template>
  <div :class="align === 'left' ? 'flex flex-col items-start' : 'sec-head'">
    <p v-if="en" class="eyebrow">{{ en }}</p>
    <component :is="as ?? 'h2'" :id="headingId" class="sec-title">{{ title }}</component>
    <span class="sec-rule" aria-hidden="true" />
    <p v-if="lead" class="sec-lead" :class="align === 'left' ? '' : 'mx-auto'">{{ lead }}</p>
    <slot />
  </div>
</template>
