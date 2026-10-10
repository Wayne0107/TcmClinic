<script setup lang="ts">
// 麵包屑：畫面 + BreadcrumbList JSON-LD
const props = defineProps<{ items: { name: string; path?: string }[] }>()
const { siteUrl } = useRuntimeConfig().public
const route = useRoute()

const all = computed(() => [{ name: '首頁', path: '/' }, ...props.items])
useJsonLd(() =>
  schema.breadcrumbs(
    all.value.map((i) => ({ name: i.name, path: i.path ?? route.path })),
    siteUrl,
  ),
)
</script>

<template>
  <nav aria-label="麵包屑" class="text-[13px] tracking-[0.06em] text-muted">
    <ol class="flex flex-wrap items-center gap-x-2 gap-y-1">
      <li v-for="(item, i) in all" :key="i" class="flex items-center gap-2">
        <span v-if="i > 0" aria-hidden="true" class="text-blush">/</span>
        <NuxtLink v-if="item.path && i < all.length - 1" :to="item.path" class="hover:text-rose">{{
          item.name
        }}</NuxtLink>
        <span v-else aria-current="page" class="text-body">{{ item.name }}</span>
      </li>
    </ol>
  </nav>
</template>
