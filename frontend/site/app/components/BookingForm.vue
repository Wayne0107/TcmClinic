<script setup lang="ts">
import type { AppointmentRequest } from '#shared/types/api'
import { BOOKING_MAX_DAYS_AHEAD, hkDateString, NOTES_MAX_LENGTH } from '#shared/utils/booking'

// 網上預約表單：欄位、標籤、提示文字與舊站 syttcm.com 相同。
// 必填檢查交由瀏覽器原生驗證（同舊站）；送出後顯示伺服器回傳的訊息，成功時清空表單。
const props = defineProps<{ sourcePage: string }>()

const { data: options } = await useBookingOptions()
const route = useRoute()

const initialService = Number(route.query.service) || ''
const form = reactive({
  name: '',
  phone: '',
  branchId: '' as number | '',
  date: '',
  slot: '' as AppointmentRequest['slot'] | '',
  serviceId: initialService as number | '',
  notes: '',
  consent: false,
  website: '',
})

// 日期下限以香港時間計算，只在瀏覽器設定，避免伺服器快取跨日
const minDate = ref('')
const maxDate = ref('')
onMounted(() => {
  minDate.value = hkDateString()
  const max = new Date(`${minDate.value}T12:00:00+08:00`)
  max.setUTCDate(max.getUTCDate() + BOOKING_MAX_DAYS_AHEAD)
  maxDate.value = max.toISOString().slice(0, 10)
})

const submitting = ref(false)
const status = ref<{ ok: boolean; message: string } | null>(null)
const statusEl = useTemplateRef<HTMLDivElement>('statusEl')

async function submit() {
  submitting.value = true
  status.value = null
  const result = await submitAppointment({
    name: form.name,
    phone: form.phone,
    branchId: Number(form.branchId),
    date: form.date,
    slot: form.slot as AppointmentRequest['slot'],
    serviceId: Number(form.serviceId),
    notes: form.notes || undefined,
    consent: form.consent,
    website: form.website,
    sourcePage: props.sourcePage,
  })
  submitting.value = false
  const fieldErrors = Object.values(result.errors ?? {}).filter(Boolean)
  status.value = {
    ok: result.ok,
    message: [result.message || '系統暫時未能處理要求。', ...fieldErrors].join(' '),
  }
  if (result.ok) {
    Object.assign(form, {
      name: '',
      phone: '',
      branchId: '',
      date: '',
      slot: '',
      serviceId: '',
      notes: '',
      consent: false,
    })
  }
  await nextTick()
  statusEl.value?.scrollIntoView({ behavior: 'smooth', block: 'center' })
}

const field =
  'mt-2 block w-full border border-line bg-white px-4 py-3 text-[15px] text-body outline-none transition-colors focus:border-cocoa'
const label = 'text-[14px] font-medium text-cocoa-2'
</script>

<template>
  <form class="space-y-5" @submit.prevent="submit">
    <div class="hidden" aria-hidden="true">
      <label
        >網站<input
          v-model="form.website"
          type="text"
          name="website"
          tabindex="-1"
          autocomplete="off"
      /></label>
    </div>

    <div
      v-if="status"
      ref="statusEl"
      role="status"
      class="px-4 py-3 text-[14.5px] leading-7"
      :class="status.ok ? 'bg-blush-bg text-cocoa-2' : 'bg-[#fff0ee] text-[#a72d20]'"
    >
      {{ status.message }}
    </div>

    <div class="grid gap-5 sm:grid-cols-2">
      <div>
        <label for="bk-name" :class="label">姓名 *</label>
        <input
          id="bk-name"
          v-model.trim="form.name"
          required
          maxlength="80"
          autocomplete="name"
          placeholder="您的姓名"
          :class="field"
        />
      </div>
      <div>
        <label for="bk-phone" :class="label">聯絡電話 *</label>
        <input
          id="bk-phone"
          v-model.trim="form.phone"
          required
          type="tel"
          maxlength="24"
          autocomplete="tel"
          placeholder="聯絡電話"
          :class="field"
        />
      </div>
    </div>

    <div class="grid gap-5 sm:grid-cols-2">
      <div>
        <label for="bk-branch" :class="label">選擇分店 *</label>
        <select id="bk-branch" v-model="form.branchId" required :class="field">
          <option value="">請選擇</option>
          <option v-for="b in options?.branches" :key="b.id" :value="b.id">{{ b.name }}</option>
        </select>
      </div>
      <div>
        <label for="bk-date" :class="label">預約日期 *</label>
        <input
          id="bk-date"
          v-model="form.date"
          required
          type="date"
          :min="minDate || undefined"
          :max="maxDate || undefined"
          :class="field"
        />
      </div>
    </div>

    <div class="grid gap-5 sm:grid-cols-2">
      <div>
        <label for="bk-slot" :class="label">預約時段 *</label>
        <select id="bk-slot" v-model="form.slot" required :class="field">
          <option value="">請選擇</option>
          <option value="morning">上午 (10:30–13:30)</option>
          <option value="afternoon">下午 (14:30–19:30)</option>
        </select>
      </div>
      <div>
        <label for="bk-service" :class="label">診療項目 *</label>
        <select id="bk-service" v-model="form.serviceId" required :class="field">
          <option value="">請選擇</option>
          <option v-for="s in options?.services" :key="s.id" :value="s.id">{{ s.name }}</option>
        </select>
      </div>
    </div>

    <div>
      <label for="bk-notes" :class="label">備註／症狀描述</label>
      <textarea
        id="bk-notes"
        v-model="form.notes"
        rows="3"
        :maxlength="NOTES_MAX_LENGTH"
        placeholder="簡述您的不適或需要（可選）"
        :class="field"
      />
    </div>

    <label class="flex cursor-pointer items-start gap-3 text-[14px] leading-7">
      <input
        v-model="form.consent"
        type="checkbox"
        required
        class="mt-1.5 size-4 shrink-0 accent-[#7d5b4e]"
      />
      <span>我同意診所使用以上資料處理及聯絡此預約。*</span>
    </label>

    <button type="submit" class="btn-primary w-full" :disabled="submitting">
      {{ submitting ? '提交中…' : '確認提交預約' }}
    </button>
    <p class="text-center text-[13px] text-muted">
      送出後診所會以電話或 WhatsApp 聯絡確認，提交並不代表預約已確定。
    </p>
  </form>
</template>
