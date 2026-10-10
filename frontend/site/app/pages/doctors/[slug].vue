<script setup lang="ts">
// 醫師個人頁：簡介與條列為舊站 /about 醫師團隊原文；應診時間待診所提供（有資料才顯示排班表）
const route = useRoute()
const slug = route.params.slug as string

const [{ data: doctor }, { data: clinic }, { data: about }] = await Promise.all([
  useDoctor(slug),
  useClinic(),
  useAboutContent(),
])
if (!doctor.value) throw createError({ statusCode: 404, message: '找不到醫師', fatal: true })
const d = doctor.value
const { siteUrl } = useRuntimeConfig().public
const scheduleFaq = computed(() => about.value?.faq.items.find((f) => f.q.includes('駐診時間')))

useSiteSeo({
  title: `${d.displayName}｜${d.headline}`,
  description: d.bio[0]?.replace(/\*\*|\[|\]\([^)]*\)/g, '') ?? d.headline,
  path: `/doctors/${d.slug}`,
  type: 'profile',
  image: d.photo ? `/images/${d.photo.src}-667.webp` : undefined,
})
useJsonLd(() => schema.physician(d, siteUrl))
</script>

<template>
  <div v-if="doctor && clinic">
    <section class="bg-blush-bg">
      <div class="wrap grid items-center gap-10 py-10 md:grid-cols-[1.2fr_1fr] md:py-16">
        <div>
          <SiteBreadcrumbs
            :items="[
              { name: '關於我們', path: '/about' },
              { name: '醫師團隊', path: '/doctors' },
              { name: doctor.name },
            ]"
          />
          <h1
            class="mt-8 font-serif text-[2.2rem] font-bold tracking-[0.16em] text-cocoa md:text-5xl"
          >
            {{ doctor.displayName }}
          </h1>
          <p class="mt-4 text-[16px] text-muted">{{ doctor.headline }}</p>
        </div>
        <div class="mx-auto w-full max-w-[360px]">
          <ResponsiveImage
            v-if="doctor.photo"
            :src="doctor.photo.src"
            :alt="doctor.photo.alt"
            sizes="(min-width: 768px) 360px, 85vw"
            priority
            class="arch aspect-[4/5] w-full object-cover object-top"
          />
          <div
            v-else
            class="ph arch grid aspect-[4/5] w-full place-items-center"
            aria-hidden="true"
          >
            <span class="font-serif text-8xl text-rose/80">{{ doctor.name.slice(0, 1) }}</span>
          </div>
        </div>
      </div>
    </section>

    <div class="wrap-narrow space-y-12 py-16 lg:py-20">
      <div class="space-y-5">
        <RichText v-for="p in doctor.bio" :key="p" :text="p" tag="p" class="prose-tcm" />
      </div>
      <ul class="space-y-2.5 text-[15px] leading-7">
        <li v-for="h in doctor.highlights" :key="h" class="flex gap-2">
          <span aria-hidden="true" class="text-blush">◆</span>{{ h }}
        </li>
      </ul>

      <section v-if="doctor.schedule?.length" aria-label="應診時間">
        <DoctorSchedule :schedule="doctor.schedule" :branches="clinic.branches" />
      </section>
      <div v-else-if="scheduleFaq" class="bg-blush-bg p-6">
        <h2 class="font-sans text-lg font-bold tracking-[0.08em] text-cocoa">
          {{ scheduleFaq.q }}
        </h2>
        <RichText :text="scheduleFaq.a" tag="p" class="mt-3 text-[15px] leading-8" />
      </div>

      <NuxtLink to="/booking" class="btn-primary">立即網上預約 →</NuxtLink>
    </div>
  </div>
</template>
