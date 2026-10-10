<script setup lang="ts">
import type { SectionSlot } from '#shared/types/api'

// 內容區塊中插入的元件；資料經由 composable 取得（docs/FRONTEND.md §3）
const props = defineProps<{ slotName: SectionSlot }>()
const { data: clinic } = await useClinic()
const { data: doctors } = props.slotName === 'doctors' ? await useDoctors() : { data: ref(null) }

// 診所相片（舊站聯絡頁相簿的相片與說明文字）
const photos = [
  { src: 'storefront-sign-1', alt: '診所門面招牌' },
  { src: 'entrance-door-2', alt: '診所門面招牌' },
  { src: 'storefront-sign-3', alt: '診所門面招牌' },
  { src: 'entrance-door-3', alt: '診所門面招牌' },
  { src: 'consult-room', alt: '診症室' },
  { src: 'notice-board', alt: '醫師介紹及天灸海報' },
  { src: 'reception', alt: '配藥房中藥牆' },
  { src: 'clinic-corridor', alt: '診所環境' },
  { src: 'clinic-waiting', alt: '診所環境' },
  { src: 'clinic-treatment-beds', alt: '診所環境' },
  { src: 'clinic-reception-a', alt: '診所環境' },
  { src: 'clinic-reception-b', alt: '診所環境' },
]
</script>

<template>
  <div
    v-if="slotName === 'doctors'"
    class="mx-auto grid max-w-[900px] gap-12 md:grid-cols-2 md:gap-8"
  >
    <DoctorCard v-for="d in doctors" :key="d.slug" :doctor="d" show-bio />
  </div>

  <div v-else-if="slotName === 'branches' && clinic" class="grid gap-6 lg:grid-cols-2">
    <BranchCard v-for="b in clinic.branches" :key="b.id" :branch="b" :clinic="clinic" />
  </div>

  <ClinicVideo v-else-if="slotName === 'video'" />

  <PhotoGallery v-else-if="slotName === 'gallery'" :photos="photos" />

  <div v-else-if="slotName === 'booking'" class="mx-auto max-w-[760px]">
    <BookingForm source-page="section" />
  </div>
</template>
