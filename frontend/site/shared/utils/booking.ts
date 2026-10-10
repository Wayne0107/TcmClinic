// 預約表單驗證：前台與 mock API 共用同一套規則（.NET 後端須實作相同規則）。
import type { AppointmentRequest } from '../types/api'

export const BOOKING_MAX_DAYS_AHEAD = 60
/** 備註字數上限（表單 maxlength 與驗證共用） */
export const NOTES_MAX_LENGTH = 1000
export const SLOT_LABELS = { morning: '上午 10:30–13:30', afternoon: '下午 14:30–19:30' } as const

/** 香港電話：8 位數字，首位 2–9，可帶 +852 與空格 */
export function normalizeHkPhone(raw: string): string | null {
  const digits = raw.replace(/[\s-]/g, '').replace(/^(\+?852)/, '')
  return /^[2-9]\d{7}$/.test(digits) ? digits : null
}

/** 以香港時區取得 YYYY-MM-DD */
export function hkDateString(date = new Date()): string {
  return new Intl.DateTimeFormat('en-CA', { timeZone: 'Asia/Hong_Kong' }).format(date)
}

export function isBookableDate(ymd: string, today = hkDateString()): boolean {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(ymd) || ymd < today) return false
  const noon = new Date(`${ymd}T12:00:00+08:00`)
  if (Number.isNaN(noon.getTime())) return false
  if (noon.getUTCDay() === 0) return false // 星期日休診
  const diff =
    (Date.parse(`${ymd}T00:00:00+08:00`) - Date.parse(`${today}T00:00:00+08:00`)) / 86_400_000
  return diff <= BOOKING_MAX_DAYS_AHEAD
}

export function validateAppointment(
  input: Partial<AppointmentRequest>,
): Partial<Record<keyof AppointmentRequest, string>> {
  const errors: Partial<Record<keyof AppointmentRequest, string>> = {}
  const name = input.name?.trim() ?? ''
  if (!name) errors.name = '請填寫姓名'
  else if (name.length > 80) errors.name = '姓名不可超過 80 字'
  if (!input.phone || !normalizeHkPhone(input.phone)) errors.phone = '請填寫 8 位數字的香港電話號碼'
  if (!input.branchId) errors.branchId = '請選擇分店'
  if (!input.serviceId) errors.serviceId = '請選擇診療項目'
  if (!input.date) errors.date = '請選擇日期'
  else if (!isBookableDate(input.date))
    errors.date = `請選擇今日起 ${BOOKING_MAX_DAYS_AHEAD} 日內的星期一至六`
  if (input.slot !== 'morning' && input.slot !== 'afternoon') errors.slot = '請選擇時段'
  if ((input.notes?.length ?? 0) > NOTES_MAX_LENGTH)
    errors.notes = `備註不可超過 ${NOTES_MAX_LENGTH} 字`
  if (!input.consent) errors.consent = '請先同意私隱政策'
  return errors
}
