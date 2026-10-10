<script setup lang="ts">
import type { Doctor } from '#shared/types/api'

// 醫師卡：置中拱形相片（同 prototype/blush.html）；文字為舊站醫師團隊原文
withDefaults(defineProps<{ doctor: Doctor; headingLevel?: 'h2' | 'h3'; showBio?: boolean }>(), {
  headingLevel: 'h3',
  showBio: false,
})
</script>

<template>
  <article class="flex flex-col items-center text-center">
    <NuxtLink
      :to="`/doctors/${doctor.slug}`"
      class="block w-full max-w-[280px]"
      tabindex="-1"
      aria-hidden="true"
    >
      <ResponsiveImage
        v-if="doctor.photo"
        :src="doctor.photo.src"
        :alt="doctor.photo.alt"
        sizes="280px"
        class="arch aspect-[4/5] w-full object-cover object-top"
      />
      <div v-else class="ph arch grid aspect-[4/5] w-full place-items-center">
        <span class="font-serif text-5xl text-rose/80">{{ doctor.name.slice(0, 1) }}</span>
      </div>
    </NuxtLink>
    <component
      :is="headingLevel"
      class="mt-6 font-sans text-xl font-bold tracking-[0.12em] text-cocoa"
    >
      <NuxtLink :to="`/doctors/${doctor.slug}`" class="hover:text-rose">{{
        doctor.displayName
      }}</NuxtLink>
    </component>
    <p class="mt-1 text-sm text-muted">{{ doctor.headline }}</p>
    <div v-if="showBio" class="mt-5 space-y-3 text-left text-[15px] leading-8">
      <RichText v-for="p in doctor.bio" :key="p" :text="p" tag="p" />
    </div>
    <ul class="mt-5 space-y-1.5 text-left text-sm leading-7">
      <li v-for="h in doctor.highlights" :key="h" class="flex gap-2">
        <span aria-hidden="true" class="text-blush">◆</span>{{ h }}
      </li>
    </ul>
  </article>
</template>
