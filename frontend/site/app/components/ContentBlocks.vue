<script setup lang="ts">
import type { ContentBlock } from '#shared/types/api'

// 內容區塊：文字全部來自資料（舊站原文），此元件只負責排版
const props = defineProps<{ blocks: ContentBlock[] }>()

// 標題層級不可跳級（WCAG）：原文 H2 後直接接 H4 時，語意上輸出為 h3，外觀維持 H4 樣式
const h4Tag = (index: number) =>
  props.blocks.slice(0, index).some((b) => b.type === 'h3') ? 'h4' : 'h3'
</script>

<template>
  <div class="space-y-6">
    <template v-for="(b, i) in blocks" :key="i">
      <RichText
        v-if="b.type === 'p'"
        :text="b.text"
        tag="p"
        :class="
          b.tone === 'note'
            ? 'border-l-2 border-blush bg-blush-bg/70 px-4 py-3 text-[13.5px] leading-7 text-muted'
            : b.tone === 'muted'
              ? 'prose-tcm text-muted'
              : 'prose-tcm'
        "
      />

      <h3
        v-else-if="b.type === 'h3'"
        class="pt-4 font-sans text-xl font-bold tracking-[0.04em] text-cocoa"
      >
        {{ b.text }}
      </h3>

      <component
        :is="h4Tag(i)"
        v-else-if="b.type === 'h4'"
        class="pt-2 font-sans text-[17px] font-bold tracking-[0.04em] text-cocoa-2"
      >
        {{ b.text }}
      </component>

      <component :is="b.ordered ? 'ol' : 'ul'" v-else-if="b.type === 'list'" class="space-y-3">
        <li v-for="(it, j) in b.items" :key="j" class="flex gap-3 text-[15px] leading-8">
          <span
            v-if="b.ordered"
            class="mt-1 grid size-6 shrink-0 place-items-center rounded-full bg-petal text-xs font-semibold text-cocoa-2"
            >{{ j + 1 }}</span
          >
          <span v-else aria-hidden="true" class="mt-3.5 size-1.5 shrink-0 rounded-full bg-blush" />
          <span
            ><strong v-if="it.title" class="font-semibold text-cocoa-2">{{ it.title }}：</strong
            ><RichText :text="it.text"
          /></span>
        </li>
      </component>

      <figure v-else-if="b.type === 'table'" class="overflow-hidden border border-line">
        <figcaption
          v-if="b.caption"
          class="bg-white px-4 pt-4 pb-2 text-sm font-semibold tracking-[0.06em] text-cocoa"
        >
          {{ b.caption }}
        </figcaption>
        <div
          class="relative overflow-x-auto"
          tabindex="0"
          role="region"
          :aria-label="b.caption ?? b.head.join('、')"
        >
          <table class="table-tcm">
            <thead>
              <tr>
                <th v-for="head in b.head" :key="head" scope="col">{{ head }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(row, r) in b.rows" :key="r">
                <td v-for="(cell, c) in row" :key="c"><RichText :text="cell" /></td>
              </tr>
            </tbody>
          </table>
        </div>
      </figure>

      <aside
        v-else-if="b.type === 'callout'"
        :class="b.tone === 'warn' ? 'border-warn/30 bg-warn-bg' : 'border-blush/40 bg-blush-bg'"
        class="border px-5 py-4 md:px-6"
      >
        <p
          v-if="b.title"
          :class="b.tone === 'warn' ? 'text-warn' : 'text-cocoa'"
          class="mb-1 text-[15px] font-semibold tracking-[0.06em]"
        >
          {{ b.title }}
        </p>
        <RichText :text="b.text" tag="p" class="text-[14.5px] leading-7" />
      </aside>

      <ol
        v-else-if="b.type === 'steps'"
        class="relative ml-3 space-y-6 border-l border-blush/60 pl-7"
      >
        <li v-for="(s, j) in b.items" :key="j" class="relative">
          <span
            class="absolute top-0.5 -left-[42px] grid size-7 place-items-center rounded-full bg-cocoa text-xs font-semibold text-white"
            >{{ j + 1 }}</span
          >
          <p class="font-sans text-[16.5px] font-bold tracking-[0.04em] text-cocoa-2">
            {{ s.title }}
          </p>
          <RichText :text="s.text" tag="p" class="mt-1 text-[15px] leading-7" />
        </li>
      </ol>

      <div v-else-if="b.type === 'cards'" class="grid gap-4 sm:grid-cols-2">
        <div v-for="(c, j) in b.items" :key="j" class="card p-5">
          <span v-if="c.tag" class="tag mb-2">{{ c.tag }}</span>
          <p class="font-sans text-[17px] font-bold tracking-[0.04em] text-cocoa-2">
            {{ c.title }}
          </p>
          <RichText :text="c.text" tag="p" class="mt-2 text-[14.5px] leading-7" />
        </div>
      </div>

      <ul v-else-if="b.type === 'tags'" class="flex flex-wrap gap-2">
        <li v-for="t in b.items" :key="t" class="tag px-3.5 py-1.5 text-[13px]">{{ t }}</li>
      </ul>

      <ul v-else-if="b.type === 'links'" class="flex flex-wrap gap-2">
        <li v-for="l in b.items" :key="l.label + l.to">
          <NuxtLink
            :to="l.to"
            class="inline-block rounded-full border border-line px-4 py-2 text-[14px] text-cocoa-2 transition-colors hover:border-cocoa hover:bg-cocoa hover:text-white"
            >{{ l.label }}</NuxtLink
          >
        </li>
      </ul>

      <dl v-else-if="b.type === 'prices'" class="flex flex-wrap gap-3">
        <div v-for="p in b.items" :key="p.label" class="border border-line bg-white px-5 py-3">
          <dt class="text-[13px] tracking-[0.06em] text-muted">{{ p.label }}</dt>
          <dd class="font-serif text-2xl font-bold text-blush-ink">{{ p.price }}</dd>
        </div>
      </dl>
    </template>
  </div>
</template>
