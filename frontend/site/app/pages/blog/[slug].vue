<script setup lang="ts">
// 中醫健康資訊文章：內文與頁面文字同舊站（發布日期、免責提示、返回連結）
const route = useRoute()
const slug = route.params.slug as string

const { data: post } = await usePost(slug)
if (!post.value) throw createError({ statusCode: 404, message: '找不到文章', fatal: true })
const p = post.value
const { siteUrl } = useRuntimeConfig().public

useSiteSeo({
  title: p.seoTitle,
  description: p.metaDescription,
  path: `/blog/${p.slug}`,
  type: 'article',
  image: `/images/${p.cover.src}-1200.webp`,
  imageAlt: p.cover.alt,
  publishedTime: p.publishedAt,
  modifiedTime: p.updatedAt,
})
useJsonLd(() => ({
  '@type': 'BlogPosting',
  '@id': `${siteUrl}/blog/${p.slug}#article`,
  headline: p.title,
  description: p.excerpt,
  image: `${siteUrl}/images/${p.cover.src}-1200.webp`,
  datePublished: p.publishedAt,
  dateModified: p.updatedAt,
  inLanguage: 'zh-Hant-HK',
  author: { '@type': 'Organization', name: p.authorName },
  publisher: { '@id': `${siteUrl}/#clinic` },
  mainEntityOfPage: `${siteUrl}/blog/${p.slug}`,
}))

const html = sanitizeArticleHtml(p.contentHtml)
</script>

<template>
  <article v-if="post">
    <header class="bg-blush-bg">
      <div class="wrap-narrow py-10 md:py-16">
        <SiteBreadcrumbs :items="[{ name: '中醫健康資訊', path: '/blog' }, { name: post.title }]" />
        <h1
          class="mt-8 font-serif text-[1.85rem] leading-[1.45] font-bold tracking-[0.06em] text-cocoa md:text-[2.4rem]"
        >
          {{ post.title }}
        </h1>
        <p class="mt-5 text-[14px] text-muted">
          <time :datetime="post.publishedAt.slice(0, 10)"
            >發布：{{ formatDate(post.publishedAt) }}</time
          >
        </p>
      </div>
    </header>

    <div class="wrap-narrow">
      <ResponsiveImage
        :src="post.cover.src"
        :alt="post.cover.alt"
        sizes="(min-width: 820px) 760px, 92vw"
        priority
        class="mt-10 aspect-[16/9] w-full object-cover"
      />
    </div>

    <div class="wrap-narrow py-12 md:py-16">
      <!-- eslint-disable-next-line vue/no-v-html -- 內文已以 DOMPurify 消毒（sanitizeArticleHtml），只保留文章標籤 -->
      <div class="prose-tcm" v-html="html" />

      <aside
        class="mt-12 border-l-2 border-blush bg-blush-bg/70 px-6 py-5 text-[14px] leading-7 text-muted"
      >
        本文只作一般健康教育用途，不能取代註冊中醫師或醫生的面診、診斷與治療。
      </aside>

      <p class="mt-10">
        <NuxtLink to="/blog" class="text-link">← 返回中醫健康資訊</NuxtLink>
      </p>
    </div>
  </article>
</template>
