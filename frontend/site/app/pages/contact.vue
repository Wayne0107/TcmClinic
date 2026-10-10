<script setup lang="ts">
// 聯絡我們：文字逐字取自舊站 /contact；兩店介紹與交通取自舊站首頁「服務範圍」（server/mock/pages/info.ts）
const [{ data: clinic }, { data: page }] = await Promise.all([useClinic(), useInfoPage('contact')])

useSiteSeo({
  title: '聯絡我們 | 聖儀堂中醫診所｜觀塘裕民中心地址及預約',
  description:
    '聖儀堂中醫診所聯絡方式：觀塘牛頭角道裕民中心商場地下23B，電話 5512 3132。營業時間星期一至六，上午 10:30–13:30、下午 14:30–19:30，星期日休診。',
  exact: true,
  path: '/contact',
  image: '/images/entrance-door-3-1200.webp',
})
</script>

<template>
  <div v-if="clinic">
    <PageHero title="聯絡我們" :breadcrumbs="[{ name: '聯絡我們' }]" />

    <section class="section" aria-labelledby="touch-title">
      <div class="wrap">
        <SectionHead
          heading-id="touch-title"
          en="GET IN TOUCH"
          title="歡迎與我們聯絡"
          lead="無論預約診症或健康諮詢，都歡迎透過以下方式聯絡聖儀堂中醫診所。"
        />
        <ul class="mt-12 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <li class="card p-6 text-center">
            <span aria-hidden="true" class="text-2xl">📞</span>
            <h3 class="mt-2 font-sans text-lg font-bold tracking-[0.1em] text-cocoa">電話</h3>
            <a :href="telUrl(clinic.phoneE164)" class="mt-1 block text-cocoa-2 hover:text-rose">{{
              clinic.phone
            }}</a>
          </li>
          <li class="card p-6 text-center">
            <span aria-hidden="true" class="text-2xl">💬</span>
            <h3 class="mt-2 font-sans text-lg font-bold tracking-[0.1em] text-cocoa">WhatsApp</h3>
            <a
              :href="whatsappUrl(clinic.whatsapp)"
              target="_blank"
              rel="noopener noreferrer"
              class="mt-1 block text-cocoa-2 hover:text-rose"
              >{{ clinic.phone }}</a
            >
          </li>
          <li class="card p-6 text-center">
            <span aria-hidden="true" class="text-2xl">📍</span>
            <h3 class="mt-2 font-sans text-lg font-bold tracking-[0.1em] text-cocoa">地址</h3>
            <p v-for="b in clinic.branches" :key="b.id" class="mt-1 text-cocoa-2">{{ b.name }}</p>
          </li>
          <li class="card p-6 text-center">
            <span aria-hidden="true" class="text-2xl">🕒</span>
            <h3 class="mt-2 font-sans text-lg font-bold tracking-[0.1em] text-cocoa">營業時間</h3>
            <p class="mt-1 text-cocoa-2">星期一至六應診</p>
            <p class="text-cocoa-2">上午 10:30–13:30</p>
            <p class="text-cocoa-2">下午 14:30–19:30</p>
            <p class="text-muted">星期日休診</p>
          </li>
        </ul>
      </div>
    </section>

    <section class="section bg-blush-bg" aria-labelledby="branches-title">
      <div class="wrap">
        <SectionHead
          heading-id="branches-title"
          en="OUR LOCATIONS"
          title="分店地址"
          lead="觀塘裕民中心、藍田匯景商場，歡迎預約就診。"
        />
        <div class="mt-12 grid gap-6 lg:grid-cols-2">
          <BranchCard
            v-for="b in clinic.branches"
            :key="b.id"
            :branch="b"
            :clinic="clinic"
            heading-level="h3"
          />
        </div>
      </div>
    </section>

    <ContentSectionView
      v-for="(sec, i) in page?.sections ?? []"
      :key="sec.id ?? sec.title"
      :section="sec"
      :index="i"
    />

    <section class="section bg-stone" aria-labelledby="gallery-title">
      <div class="wrap">
        <SectionHead
          heading-id="gallery-title"
          en="GALLERY"
          title="診所環境"
          lead="點擊相片可放大瀏覽。"
        />
        <div class="mt-12"><SectionSlotView slot-name="gallery" /></div>
      </div>
    </section>

    <section class="section" aria-labelledby="video-title">
      <div class="wrap">
        <SectionHead
          heading-id="video-title"
          en="CLINIC VIDEO"
          title="診所影片"
          lead="透過影片率先感受聖儀堂的環境與氛圍。"
        />
        <div class="mt-12"><ClinicVideo /></div>
      </div>
    </section>

    <CtaBand v-if="page?.cta" :cta="page.cta" />
  </div>
</template>
