<script setup lang="ts">
import type { Branch, DoctorSchedule } from '#shared/types/api'

// 醫師應診時間表：列 = 星期一至六，欄 = 上午／下午，格內顯示分店
const props = defineProps<{
  schedule: DoctorSchedule[]
  branches: Pick<Branch, 'id' | 'shortName'>[]
  compact?: boolean
}>()

const days = [1, 2, 3, 4, 5, 6]
const today = ref<number | null>(null)
onMounted(() => {
  today.value = new Date().getDay()
})

function cell(day: number, slot: 'morning' | 'afternoon') {
  const s = props.schedule.find((x) => x.day === day && x.slot === slot)
  return s ? props.branches.find((b) => b.id === s.branchId)?.shortName : undefined
}
</script>

<template>
  <div
    class="relative overflow-x-auto border border-line"
    tabindex="0"
    role="region"
    aria-label="應診時間表"
  >
    <table class="w-full min-w-[420px] text-center text-[14px]">
      <caption class="sr-only">
        應診時間（示範資料，待診所提供）
      </caption>
      <thead class="bg-blush-bg text-cocoa-2">
        <tr>
          <th scope="col" class="px-3 py-2.5 font-semibold">時段</th>
          <th
            v-for="d in days"
            :key="d"
            scope="col"
            class="px-2 py-2.5 font-semibold"
            :class="today === d ? 'text-rose' : ''"
          >
            {{ WEEKDAYS_SHORT[d] }}<span v-if="today === d" class="sr-only">（今日）</span>
          </th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="slot in ['morning', 'afternoon'] as const"
          :key="slot"
          class="border-t border-line"
        >
          <th scope="row" class="px-3 py-3 text-left font-medium whitespace-nowrap text-cocoa-2">
            {{ slot === 'morning' ? '上午' : '下午' }}
            <span v-if="!compact" class="block text-xs font-normal text-muted">{{
              slot === 'morning' ? '10:30–13:30' : '14:30–19:30'
            }}</span>
          </th>
          <td
            v-for="d in days"
            :key="d"
            class="px-2 py-3"
            :class="today === d ? 'bg-blush-bg/60' : ''"
          >
            <span v-if="cell(d, slot)" class="tag px-2">{{ cell(d, slot) }}</span>
            <span v-else class="text-muted" aria-label="不應診">—</span>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>
