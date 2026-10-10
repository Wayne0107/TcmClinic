<script setup lang="ts">
const route = useRoute()
const page = Number(route.params.n)
// /blog/page/1 與非數字頁碼：導回正確網址
if (!Number.isInteger(page) || page < 1)
  throw createError({ statusCode: 404, message: '找不到此頁', fatal: true })
if (page === 1) await navigateTo('/blog', { redirectCode: 301 })
</script>

<template>
  <BlogListing :page="page" />
</template>
