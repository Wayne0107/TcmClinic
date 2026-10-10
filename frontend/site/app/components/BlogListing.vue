<script setup lang="ts">
// 中醫健康資訊列表：/blog、/blog/page/n（網址、每頁 15 篇、標題文字同舊站）
const props = defineProps<{ page: number }>()
const PAGE_SIZE = 15

const { data: posts, error } = await usePosts({ page: props.page, pageSize: PAGE_SIZE })
if (error.value || !posts.value)
  throw createError({ statusCode: 404, message: '找不到此頁', fatal: true })

const { siteUrl } = useRuntimeConfig().public
useSiteSeo({
  title:
    props.page > 1
      ? `中醫健康資訊 Blog｜第 ${props.page} 頁｜聖儀堂中醫診所`
      : '中醫健康資訊 Blog｜聖儀堂中醫診所',
  description: '聖儀堂中醫診所分享中醫婦科、中醫美容、產後調理、健康養生及減肥修身資訊。',
  exact: true,
  path: props.page > 1 ? `/blog/page/${props.page}` : '/blog',
})
useJsonLd(() => ({
  '@type': 'CollectionPage',
  name: '中醫健康資訊',
  url: `${siteUrl}/blog`,
  mainEntity: {
    '@type': 'ItemList',
    itemListElement: posts.value!.items.map((p, i) => ({
      '@type': 'ListItem',
      position: (props.page - 1) * PAGE_SIZE + i + 1,
      url: `${siteUrl}/blog/${p.slug}`,
      name: p.title,
    })),
  },
}))

const featured = computed(() => (props.page === 1 ? posts.value!.items[0] : undefined))
const rest = computed(() => (featured.value ? posts.value!.items.slice(1) : posts.value!.items))
</script>

<template>
  <div v-if="posts">
    <PageHero
      eyebrow="HEALTH JOURNAL"
      title="中醫健康資訊"
      lead="分享中醫婦科、美容、養生及體重管理知識。"
      :breadcrumbs="[{ name: '中醫健康資訊' }]"
    />

    <section class="section bg-stone pt-12 md:pt-16" aria-label="中醫健康資訊列表">
      <div class="wrap">
        <PostCard v-if="featured" :post="featured" featured heading-level="h2" class="mb-8" />
        <ul class="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
          <li v-for="p in rest" :key="p.slug"><PostCard :post="p" heading-level="h2" /></li>
        </ul>
        <PaginationNav
          :page="posts.page"
          :total-pages="Math.ceil(posts.totalCount / posts.pageSize)"
          base-path="/blog"
        />
      </div>
    </section>
  </div>
</template>
