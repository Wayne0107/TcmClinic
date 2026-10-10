<script setup lang="ts">
import type { Branch, ClinicInfo } from '#shared/types/api'

// 分店卡：欄位名稱與按鈕文字同舊站聯絡我們頁
defineProps<{ branch: Branch; clinic: ClinicInfo; headingLevel?: 'h2' | 'h3' }>()
</script>

<template>
  <article :id="branch.slug" class="card grid overflow-hidden sm:grid-cols-[200px_1fr]">
    <ResponsiveImage
      :src="branch.image.src"
      :alt="branch.image.alt"
      sizes="(min-width: 640px) 200px, 90vw"
      class="aspect-[4/3] size-full object-cover sm:aspect-auto"
    />
    <div class="p-6">
      <p class="text-[14px] tracking-[0.1em] text-blush-ink">{{ branch.label }}</p>
      <component
        :is="headingLevel ?? 'h3'"
        class="mt-1 font-sans text-xl font-bold tracking-[0.12em] text-cocoa"
      >
        {{ branch.name }}
      </component>
      <dl class="mt-4 space-y-2 text-[14.5px] leading-7">
        <div class="flex gap-3">
          <dt class="w-10 shrink-0 text-muted">地址</dt>
          <dd>{{ branch.address }}</dd>
        </div>
        <div class="flex gap-3">
          <dt class="w-10 shrink-0 text-muted">交通</dt>
          <dd>{{ branch.transport }}</dd>
        </div>
        <div class="flex gap-3">
          <dt class="w-10 shrink-0 text-muted">電話</dt>
          <dd>
            <a :href="telUrl(clinic.phoneE164)" class="text-cocoa hover:text-rose">{{
              clinic.phone
            }}</a>
          </dd>
        </div>
        <div class="flex gap-3">
          <dt class="w-10 shrink-0 text-muted">營業</dt>
          <dd>{{ clinic.hoursText }}<br />{{ clinic.closedNote }}</dd>
        </div>
      </dl>
      <div class="mt-5 flex flex-wrap gap-2">
        <a
          :href="whatsappUrl(clinic.whatsapp, branch.whatsappText)"
          target="_blank"
          rel="noopener noreferrer"
          class="btn-wa min-h-10 px-5 py-1.5 text-sm"
          >WhatsApp 預約</a
        >
        <a
          :href="mapUrl(branch.mapQuery)"
          target="_blank"
          rel="noopener noreferrer"
          class="btn-ghost min-h-10 px-5 py-1.5 text-sm"
          >Google 導航<span class="sr-only">：{{ branch.name }}</span></a
        >
      </div>
    </div>
  </article>
</template>
