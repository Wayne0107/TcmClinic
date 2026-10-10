<script setup lang="ts">
// 深色頁尾（版面同 prototype/blush.html；文字取自舊站 syttcm.com 頁尾原文）
const { data: clinic } = await useClinic()

const quickLinks = [
  { label: '首頁', to: '/' },
  { label: '關於我們・醫師資歷', to: '/about' },
  { label: '診療服務一覽', to: '/services' },
  { label: '常見問題', to: '/#faq' },
  { label: '中醫健康資訊', to: '/blog' },
  { label: '聯絡我們・地址交通', to: '/contact' },
]
const serviceLinks = [
  { label: '中醫婦科・不孕不育', to: '/gynecology' },
  { label: '懷孕安胎・產後調理', to: '/gynecology#postnatal' },
  { label: '中醫美容・中藥面膜', to: '/beauty' },
  { label: '面部經絡理療・美容針', to: '/beauty#needle' },
  { label: '減肥修身・茶療減肥', to: '/slim' },
  { label: '中醫全科・皮膚調理', to: '/services#general' },
]
</script>

<template>
  <footer v-if="clinic" id="clinic-info" class="bg-slate text-on-slate">
    <div class="wrap grid gap-12 py-20 md:grid-cols-2 md:py-24 lg:grid-cols-[1.3fr_1fr_1fr_1.3fr]">
      <div>
        <h2 class="font-serif text-lg font-semibold tracking-[0.14em]">{{ clinic.name }}</h2>
        <p class="mt-1 font-latin text-sm tracking-[0.08em] text-slate-muted">
          {{ clinic.nameEn }}
        </p>
        <p class="mt-4 text-sm leading-[1.9] text-slate-muted">{{ clinic.description }}</p>
      </div>

      <nav aria-labelledby="footer-quick">
        <h2 id="footer-quick" class="text-sm tracking-[0.14em] text-blush">快速連結</h2>
        <ul class="mt-4 space-y-2.5 text-[14px]">
          <li v-for="l in quickLinks" :key="l.to">
            <NuxtLink :to="l.to" class="hover:text-blush">{{ l.label }}</NuxtLink>
          </li>
        </ul>
      </nav>

      <nav aria-labelledby="footer-services">
        <h2 id="footer-services" class="text-sm tracking-[0.14em] text-blush">診療範疇</h2>
        <ul class="mt-4 space-y-2.5 text-[14px]">
          <li v-for="l in serviceLinks" :key="l.to">
            <NuxtLink :to="l.to" class="hover:text-blush">{{ l.label }}</NuxtLink>
          </li>
        </ul>
      </nav>

      <div>
        <h2 class="text-sm tracking-[0.14em] text-blush">聯絡預約</h2>
        <ul class="mt-4 space-y-2.5 text-[14px] leading-7">
          <li>
            電話：
            <a :href="telUrl(clinic.phoneE164)" class="tabular-nums hover:text-blush">{{
              clinic.phone
            }}</a>
          </li>
          <li>
            <a
              :href="whatsappUrl(clinic.whatsapp, clinic.whatsappDefaultText)"
              target="_blank"
              rel="noopener noreferrer"
              class="border-b border-blush pb-0.5 hover:text-blush"
              >WhatsApp 預約</a
            >
          </li>
          <li v-for="b in clinic.branches" :key="b.id" class="text-slate-muted">
            <span aria-hidden="true">📍 </span>{{ b.address }}
          </li>
          <li class="text-slate-muted">
            <span aria-hidden="true">🕒 </span>{{ clinic.hoursText }}；{{ clinic.closedNote }}
          </li>
        </ul>
      </div>
    </div>

    <div class="border-t border-white/10">
      <div class="wrap space-y-3 pt-6 pb-24 text-xs leading-6 text-slate-muted md:pb-6">
        <p>{{ clinic.disclaimer }}</p>
        <p>{{ clinic.copyright }}</p>
      </div>
    </div>
  </footer>
</template>
