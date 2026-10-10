<script setup lang="ts">
import type { PostSummary } from '#shared/types/api'

// 文章卡：平面白底、上圖下文（同 prototype/blush.html）；文字同舊站中醫健康資訊列表
defineProps<{ post: PostSummary; featured?: boolean; headingLevel?: 'h2' | 'h3' }>()
</script>

<template>
  <article class="group h-full bg-white" :class="featured ? 'md:grid md:grid-cols-2' : ''">
    <NuxtLink
      :to="`/blog/${post.slug}`"
      class="block overflow-hidden"
      tabindex="-1"
      aria-hidden="true"
    >
      <ResponsiveImage
        :src="post.cover.src"
        :alt="post.cover.alt"
        :sizes="
          featured
            ? '(min-width: 768px) 560px, 90vw'
            : '(min-width: 1024px) 360px, (min-width: 768px) 45vw, 90vw'
        "
        :priority="featured"
        class="aspect-[3/2] h-full w-full object-cover transition-transform duration-500 group-hover:scale-105"
      />
    </NuxtLink>
    <div class="p-6" :class="featured ? 'md:flex md:flex-col md:justify-center md:p-10' : ''">
      <time
        :datetime="post.publishedAt.slice(0, 10)"
        class="text-xs tracking-[0.06em] text-blush-ink tabular-nums"
        >{{ formatDate(post.publishedAt) }}</time
      >
      <component
        :is="headingLevel ?? 'h3'"
        class="mt-2 font-sans font-bold tracking-[0.04em] text-cocoa"
        :class="featured ? 'text-2xl leading-[1.5]' : 'text-lg leading-[1.6]'"
      >
        <NuxtLink :to="`/blog/${post.slug}`" class="group-hover:text-rose">{{
          post.title
        }}</NuxtLink>
      </component>
      <p class="mt-3 text-sm leading-[1.8] text-muted" :class="featured ? '' : 'line-clamp-3'">
        {{ post.excerpt }}
      </p>
      <NuxtLink
        :to="`/blog/${post.slug}`"
        class="mt-4 inline-block text-sm font-medium tracking-[0.06em] text-cocoa hover:text-rose"
        >閱讀全文 →<span class="sr-only">：{{ post.title }}</span></NuxtLink
      >
    </div>
  </article>
</template>
