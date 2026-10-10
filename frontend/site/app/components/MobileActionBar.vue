<script setup lang="ts">
// 手機底部固定操作列：電話／WhatsApp／立即預約（文字沿用舊站）。桌面版改為左下角 WhatsApp 浮動按鈕（同 blush.html）。
const { data: clinic } = await useClinic()
const route = useRoute()
const hidden = computed(() => route.path === '/booking')
// 服務詳情頁左側有「本頁內容」目錄，浮動按鈕改放右下角以免遮住
const onRight = computed(() => ['/gynecology', '/beauty', '/slim'].includes(route.path))
</script>

<template>
  <template v-if="clinic && !hidden">
    <nav
      aria-label="快速聯絡"
      class="fixed inset-x-0 bottom-0 z-30 border-t border-line bg-white/95 pb-[env(safe-area-inset-bottom)] backdrop-blur md:hidden"
    >
      <ul class="grid grid-cols-3 text-center text-[13px] tracking-[0.08em]">
        <li>
          <a
            :href="telUrl(clinic.phoneE164)"
            class="flex min-h-14 flex-col items-center justify-center gap-0.5 text-cocoa"
          >
            <svg aria-hidden="true" viewBox="0 0 24 24" class="size-5">
              <path
                fill="none"
                stroke="currentColor"
                stroke-width="1.6"
                d="M5 4h4l2 5-2.5 1.5a11 11 0 0 0 5 5L15 13l5 2v4a2 2 0 0 1-2 2A16 16 0 0 1 3 6a2 2 0 0 1 2-2"
              />
            </svg>
            電話
          </a>
        </li>
        <li>
          <a
            :href="whatsappUrl(clinic.whatsapp, clinic.whatsappDefaultText)"
            target="_blank"
            rel="noopener noreferrer"
            class="flex min-h-14 flex-col items-center justify-center gap-0.5 text-cocoa"
          >
            <svg aria-hidden="true" viewBox="0 0 24 24" class="size-5">
              <path
                fill="none"
                stroke="currentColor"
                stroke-width="1.6"
                d="M4 20l1.3-3.9A8 8 0 1 1 8 19.2L4 20Z"
              />
            </svg>
            WhatsApp
          </a>
        </li>
        <li>
          <NuxtLink
            to="/booking"
            class="flex min-h-14 flex-col items-center justify-center gap-0.5 bg-cocoa text-white"
          >
            <svg aria-hidden="true" viewBox="0 0 24 24" class="size-5">
              <path
                fill="none"
                stroke="currentColor"
                stroke-width="1.6"
                d="M7 3v3M17 3v3M4 9h16M5 5h14a1 1 0 0 1 1 1v13a1 1 0 0 1-1 1H5a1 1 0 0 1-1-1V6a1 1 0 0 1 1-1Z"
              />
            </svg>
            立即預約
          </NuxtLink>
        </li>
      </ul>
    </nav>

    <a
      :href="whatsappUrl(clinic.whatsapp, clinic.whatsappDefaultText)"
      target="_blank"
      rel="noopener noreferrer"
      class="btn-wa fixed bottom-6 z-30 hidden shadow-[0_10px_26px_rgb(18_140_73/0.3)] md:inline-flex"
      :class="onRight ? 'right-6' : 'left-6'"
    >
      <svg aria-hidden="true" viewBox="0 0 24 24" class="size-5">
        <path
          fill="currentColor"
          d="M12 2a10 10 0 0 0-8.6 15.1L2 22l5-1.3A10 10 0 1 0 12 2Zm5.2 14.2c-.2.6-1.3 1.2-1.8 1.2-.5.1-1 .2-3.3-.7-2.8-1.1-4.6-4-4.7-4.2-.1-.2-1.1-1.5-1.1-2.9s.7-2.1 1-2.4c.3-.3.6-.3.8-.3h.6c.2 0 .4 0 .7.5l.9 2.2c.1.2.1.4 0 .6l-.4.6-.4.4c-.1.2-.3.3-.1.6.2.3.8 1.3 1.7 2.1 1.2 1 2.1 1.4 2.4 1.5.3.1.5.1.7-.1l.9-1.1c.2-.3.4-.2.7-.1l2 1c.3.1.5.2.5.3.1.2.1.7-.1 1.3Z"
        />
      </svg>
      WhatsApp 預約
    </a>
  </template>
</template>
