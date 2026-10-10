<script setup lang="ts">
// 分頁（同舊站只列頁碼）：第 1 頁為 basePath，其餘為 `${basePath}/page/${n}`（沿用舊站網址格式）
const props = defineProps<{ page: number; totalPages: number; basePath: string }>()

const href = (n: number) => (n === 1 ? props.basePath : `${props.basePath}/page/${n}`)

// 頁碼在同一個元件實例內不會改變（換頁會重新建立頁面），所以只需設定一次
const { siteUrl } = useRuntimeConfig().public
if (props.page > 1) useHead({ link: [{ rel: 'prev', href: `${siteUrl}${href(props.page - 1)}` }] })
if (props.page < props.totalPages)
  useHead({ link: [{ rel: 'next', href: `${siteUrl}${href(props.page + 1)}` }] })
</script>

<template>
  <nav v-if="totalPages > 1" aria-label="分頁" class="mt-14 flex items-center justify-center gap-2">
    <ul class="flex flex-wrap justify-center gap-1.5">
      <li v-for="n in totalPages" :key="n">
        <NuxtLink
          :to="href(n)"
          class="grid size-11 place-items-center rounded-full text-sm"
          :class="n === page ? 'bg-cocoa text-white' : 'text-cocoa hover:bg-blush-bg'"
          :aria-current="n === page ? 'page' : undefined"
          :aria-label="`第 ${n} 頁`"
          >{{ n }}</NuxtLink
        >
      </li>
    </ul>
  </nav>
</template>
