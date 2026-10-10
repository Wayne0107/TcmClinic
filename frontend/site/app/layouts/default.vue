<script setup lang="ts">
const { data: clinic } = await useClinic()
const { siteUrl } = useRuntimeConfig().public

// 全站共用的結構化資料：WebSite + 診所（含兩間分店）
useJsonLd(() =>
  clinic.value
    ? [
        {
          '@type': 'WebSite',
          '@id': `${siteUrl}/#website`,
          url: `${siteUrl}/`,
          name: clinic.value.name,
          inLanguage: 'zh-Hant-HK',
          publisher: { '@id': `${siteUrl}/#clinic` },
        },
        schema.clinic(clinic.value, siteUrl),
      ]
    : null,
)
</script>

<template>
  <div class="flex min-h-dvh flex-col">
    <a
      href="#main"
      class="sr-only z-50 rounded-full bg-cocoa px-5 py-2 text-white focus:not-sr-only focus:fixed focus:top-3 focus:left-3"
      >跳至主要內容</a
    >
    <SiteHeader />
    <main id="main" class="flex-1 pb-16 md:pb-0">
      <slot />
    </main>
    <SiteFooter />
    <MobileActionBar />
  </div>
</template>
