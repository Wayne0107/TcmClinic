<script setup lang="ts">
import type { NavLink } from '~/utils/nav'

// 頂部導覽：桌面版下拉（hover 與鍵盤 focus-within 皆可展開），「診療服務」為四欄大型選單、列出各科主題；
// 手機版全螢幕選單。選單結構見 app/utils/nav.ts。
const { data: clinic } = await useClinic()
const route = useRoute()
const open = ref(false)
const menuButton = useTemplateRef<HTMLButtonElement>('menuButton')

const isActive = (item: NavLink) => isNavActive(item, route.path)
const isMega = (item: NavLink) => item.children?.some((c) => c.children?.length) ?? false

watch(
  () => route.fullPath,
  () => {
    open.value = false
  },
)
watch(open, (v) => {
  if (import.meta.client) document.documentElement.style.overflow = v ? 'hidden' : ''
})
function onKey(e: KeyboardEvent) {
  if (e.key === 'Escape' && open.value) {
    open.value = false
    menuButton.value?.focus()
  }
}
onMounted(() => window.addEventListener('keydown', onKey))
onBeforeUnmount(() => window.removeEventListener('keydown', onKey))
</script>

<template>
  <header
    class="sticky top-0 z-40 border-b border-line/80 bg-white/95 backdrop-blur supports-[backdrop-filter]:bg-white/85"
  >
    <!-- 桌面版資訊列 -->
    <div v-if="clinic" class="hidden bg-stone md:block">
      <div
        class="wrap flex h-9 max-w-[1320px] items-center justify-between gap-6 text-[12.5px] tracking-[0.08em] whitespace-nowrap text-muted"
      >
        <p>{{ clinic.hoursText }}；{{ clinic.closedNote }}</p>
        <p class="flex items-center gap-5">
          <span>觀塘裕民中心・藍田匯景商場</span>
          <a :href="telUrl(clinic.phoneE164)" class="font-medium text-cocoa hover:text-rose"
            >電話： {{ clinic.phone }}</a
          >
        </p>
      </div>
    </div>

    <div class="wrap flex h-[68px] max-w-[1320px] items-center justify-between gap-6 md:h-20">
      <NuxtLink to="/" class="flex shrink-0 items-center gap-3 whitespace-nowrap">
        <img
          src="/logo-96.webp"
          alt=""
          width="44"
          height="44"
          class="size-11 rounded-full border border-line"
        />
        <span class="flex flex-col leading-tight">
          <span class="font-serif text-[19px] font-bold tracking-[0.18em] text-cocoa md:text-xl"
            >聖儀堂中醫診所</span
          >
          <span class="font-latin text-[13px] tracking-[0.12em] text-blush-ink"
            >SHENG YI TANG TCM</span
          >
        </span>
      </NuxtLink>

      <nav aria-label="主要導覽" class="hidden xl:block">
        <ul class="flex items-center">
          <li v-for="item in mainNav" :key="item.to" class="group relative">
            <NuxtLink
              :to="item.to"
              class="flex items-center gap-1 rounded-full px-3 py-2 text-[15px] tracking-[0.08em] whitespace-nowrap transition-colors hover:text-rose 2xl:px-4 2xl:tracking-[0.12em]"
              :class="isActive(item) ? 'text-rose' : 'text-slate'"
              :aria-current="route.path === item.to ? 'page' : undefined"
            >
              {{ item.label }}
              <svg
                v-if="item.children"
                aria-hidden="true"
                viewBox="0 0 20 20"
                class="size-3.5 opacity-60"
              >
                <path
                  fill="currentColor"
                  d="M5.3 7.3a1 1 0 0 1 1.4 0L10 10.6l3.3-3.3a1 1 0 1 1 1.4 1.4l-4 4a1 1 0 0 1-1.4 0l-4-4a1 1 0 0 1 0-1.4Z"
                />
              </svg>
            </NuxtLink>
            <div
              v-if="item.children"
              class="invisible absolute top-full left-1/2 -translate-x-1/2 pt-3 opacity-0 transition-opacity duration-150 group-focus-within:visible group-focus-within:opacity-100 group-hover:visible group-hover:opacity-100"
              :class="isMega(item) ? 'w-[800px]' : 'w-48'"
            >
              <ul
                class="border border-line border-t-2 border-t-blush bg-white shadow-[var(--shadow-soft)]"
                :class="isMega(item) ? 'grid grid-cols-4 gap-2 p-4' : 'p-2'"
              >
                <li v-for="c in item.children" :key="c.to + c.label">
                  <NuxtLink :to="c.to" class="block px-4 py-3 hover:bg-blush-bg">
                    <span
                      class="block text-[15px] tracking-[0.08em]"
                      :class="c.children ? 'font-semibold text-cocoa' : 'text-cocoa-2'"
                      >{{ c.label }}</span
                    >
                  </NuxtLink>
                  <ul v-if="c.children" class="pb-2">
                    <li v-for="g in c.children" :key="g.to">
                      <NuxtLink
                        :to="g.to"
                        class="block px-4 py-1.5 text-[14px] text-muted hover:text-rose"
                        >{{ g.label }}</NuxtLink
                      >
                    </li>
                  </ul>
                </li>
              </ul>
            </div>
          </li>
        </ul>
      </nav>

      <div class="flex shrink-0 items-center gap-2">
        <NuxtLink
          to="/booking"
          class="btn-primary hidden min-h-11 px-6 py-2.5 whitespace-nowrap sm:inline-flex"
          >立即預約</NuxtLink
        >
        <button
          ref="menuButton"
          type="button"
          class="grid size-11 place-items-center rounded-full border border-line text-cocoa xl:hidden"
          :aria-expanded="open"
          aria-controls="mobile-menu"
          :aria-label="open ? '關閉選單' : '開啟選單'"
          @click="open = !open"
        >
          <svg v-if="!open" aria-hidden="true" viewBox="0 0 24 24" class="size-5">
            <path
              fill="none"
              stroke="currentColor"
              stroke-linecap="round"
              stroke-width="1.8"
              d="M4 7h16M4 12h16M4 17h16"
            />
          </svg>
          <svg v-else aria-hidden="true" viewBox="0 0 24 24" class="size-5">
            <path
              fill="none"
              stroke="currentColor"
              stroke-linecap="round"
              stroke-width="1.8"
              d="m6 6 12 12M18 6 6 18"
            />
          </svg>
        </button>
      </div>
    </div>

    <!-- 手機版選單：header 有 backdrop-filter，fixed 會以 header 為定位基準，改用 absolute 接在 header 下方填滿視窗 -->
    <div
      v-show="open"
      id="mobile-menu"
      class="absolute inset-x-0 top-full h-[calc(100dvh-100%)] overflow-y-auto bg-white xl:hidden"
    >
      <nav aria-label="手機版導覽" class="wrap py-6">
        <ul class="divide-y divide-line">
          <li v-for="item in mainNav" :key="item.to" class="py-4">
            <NuxtLink :to="item.to" class="block font-serif text-lg tracking-[0.12em] text-cocoa">{{
              item.label
            }}</NuxtLink>
            <ul v-if="item.children" class="mt-3 grid grid-cols-2 gap-2">
              <li v-for="c in item.children" :key="c.to + c.label">
                <NuxtLink :to="c.to" class="block bg-blush-bg px-4 py-3 text-[15px] text-cocoa-2">{{
                  c.label
                }}</NuxtLink>
              </li>
            </ul>
          </li>
        </ul>
        <div v-if="clinic" class="mt-6 grid gap-3">
          <NuxtLink to="/booking" class="btn-primary">立即預約</NuxtLink>
          <a :href="telUrl(clinic.phoneE164)" class="btn-ghost">電話： {{ clinic.phone }}</a>
        </div>
      </nav>
    </div>
  </header>
</template>
