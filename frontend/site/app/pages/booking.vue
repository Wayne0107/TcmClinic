<script setup lang="ts">
// 網上預約：標題與說明沿用舊站「網上預約」區塊原文；側欄同舊站首頁預約區的「診所資訊」
const { data: clinic } = await useClinic()

useSiteSeo({
  title: '網上預約',
  // 描述由頁面上的原文句子組成
  description:
    '填妥下表後，診所會盡快以電話或 WhatsApp 聯絡確認預約。送出後診所會以電話或 WhatsApp 聯絡確認，提交並不代表預約已確定。',
  path: '/booking',
})
</script>

<template>
  <div>
    <PageHero
      eyebrow="APPOINTMENT"
      title="網上預約"
      lead="填妥下表後，診所會盡快以電話或 WhatsApp 聯絡確認預約。"
      :breadcrumbs="[{ name: '網上預約' }]"
    />

    <div class="wrap grid gap-10 py-12 lg:grid-cols-[1fr_320px] lg:py-16">
      <div class="card p-6 md:p-10">
        <BookingForm source-page="booking" />
      </div>

      <aside
        v-if="clinic"
        class="card h-fit p-6 lg:sticky lg:top-32"
        aria-labelledby="clinic-info-title"
      >
        <h2 id="clinic-info-title" class="font-sans text-lg font-bold tracking-[0.1em] text-cocoa">
          診所資訊
        </h2>
        <ul class="mt-4 space-y-4 text-[14.5px] leading-7">
          <li v-for="b in clinic.branches" :key="b.id" class="flex gap-3">
            <span aria-hidden="true">📍</span>
            <span
              ><strong class="block font-medium text-cocoa-2">{{ b.name }}</strong
              >{{ b.address === b.name ? '' : b.address }}</span
            >
          </li>
          <li class="flex gap-3">
            <span aria-hidden="true">📞</span>
            <a :href="telUrl(clinic.phoneE164)" class="hover:text-rose">{{ clinic.phone }}</a>
          </li>
          <li class="flex gap-3">
            <span aria-hidden="true">🕒</span>
            <span>{{ clinic.hoursText }}<br />{{ clinic.closedNote }}</span>
          </li>
          <li class="flex gap-3">
            <span aria-hidden="true">💬</span>
            <a
              :href="whatsappUrl(clinic.whatsapp)"
              target="_blank"
              rel="noopener noreferrer"
              class="text-cocoa underline underline-offset-4 hover:text-rose"
              >WhatsApp 預約</a
            >
          </li>
        </ul>
      </aside>
    </div>
  </div>
</template>
