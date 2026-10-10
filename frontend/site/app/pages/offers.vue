<script setup lang="ts">
// 最新優惠：內容取自舊站 /services「收費一覽」原文（舊站沒有其他優惠文案）
const { data: offers } = await useOffers()
const { siteUrl } = useRuntimeConfig().public

useSiteSeo({
  title: '最新優惠',
  // 描述由頁面上的原文句子組成
  description:
    '以下為各項服務參考收費，實際以診症評估為準。＊以上收費僅供參考，個別療程因應病情及用藥而異，詳情請致電 5512 3132 查詢。',
  path: '/offers',
  image: '/images/svc-beauty-roller-1200.webp',
})
useJsonLd(() =>
  offers.value?.items.map((o) => ({
    '@type': 'Offer',
    name: o.title,
    description: o.description,
    price: o.price.replace(/（.*）/, '').replace(/[^\d]/g, ''),
    priceCurrency: 'HKD',
    url: `${siteUrl}/offers#${o.id}`,
    seller: { '@id': `${siteUrl}/#clinic` },
  })),
)
</script>

<template>
  <div>
    <PageHero
      eyebrow="PRICE LIST"
      title="最新優惠"
      lead="以下為各項服務參考收費，實際以診症評估為準。"
      :breadcrumbs="[{ name: '最新優惠' }]"
    />
    <section class="section">
      <div class="wrap">
        <ul class="grid gap-6 md:grid-cols-2">
          <li v-for="(o, i) in offers?.items" :key="o.id">
            <OfferCard :offer="o" heading-level="h2" :tone="i" />
          </li>
        </ul>
        <div class="mt-12 space-y-2 bg-stone p-6 text-[13.5px] leading-7 text-muted md:p-8">
          <p v-for="n in offers?.notes" :key="n">{{ n }}</p>
        </div>
      </div>
    </section>
  </div>
</template>
