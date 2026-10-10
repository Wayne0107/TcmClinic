import { describe, expect, it } from 'vitest'
import type { AppointmentRequest } from '#shared/types/api'
import { isBookableDate, normalizeHkPhone, validateAppointment } from '#shared/utils/booking'

const TODAY = '2026-10-09' // 星期五

function validRequest(overrides: Partial<AppointmentRequest> = {}): Partial<AppointmentRequest> {
  return {
    name: '陳小姐',
    phone: '9123 4567',
    branchId: 1,
    date: '2026-10-12', // 星期一
    slot: 'morning',
    serviceId: 5,
    consent: true,
    sourcePage: 'test',
    ...overrides,
  }
}

describe('normalizeHkPhone', () => {
  // 情境：使用者輸入常見的香港號碼格式（含空格、+852、連字號）→ 都應轉成 8 位數字。
  it.each([
    ['91234567', '91234567'],
    ['9123 4567', '91234567'],
    ['+852 9123 4567', '91234567'],
    ['852-2345-6789', '23456789'],
  ])('接受 %s 並轉為 %s', (input, expected) => {
    expect(normalizeHkPhone(input)).toBe(expected)
  })

  // 情境：位數不對或以 0、1 開頭不是香港號碼 → 回傳 null，表單應提示錯誤。
  it.each(['1234567', '123456789', '01234567', '12345678', 'abcdefgh'])('拒絕 %s', (input) => {
    expect(normalizeHkPhone(input)).toBeNull()
  })
})

describe('isBookableDate', () => {
  // 情境：星期一至六且在 60 日內 → 可預約。
  it('接受今日起的應診日', () => {
    expect(isBookableDate('2026-10-09', TODAY)).toBe(true)
    expect(isBookableDate('2026-10-10', TODAY)).toBe(true)
  })

  // 情境：星期日診所休診 → 不可預約。
  it('拒絕星期日', () => {
    expect(isBookableDate('2026-10-11', TODAY)).toBe(false)
  })

  // 情境：過去的日期或超過 60 日 → 不可預約。
  it('拒絕過去日期與超過 60 日的日期', () => {
    expect(isBookableDate('2026-10-08', TODAY)).toBe(false)
    expect(isBookableDate('2026-12-08', TODAY)).toBe(true)
    expect(isBookableDate('2026-12-09', TODAY)).toBe(false)
  })

  // 情境：格式錯誤的字串 → 不可預約，避免送出無效資料。
  it('拒絕格式錯誤的日期', () => {
    expect(isBookableDate('2026/10/12', TODAY)).toBe(false)
    expect(isBookableDate('not-a-date', TODAY)).toBe(false)
  })
})

describe('validateAppointment', () => {
  // 情境：所有必填欄位都正確 → 沒有錯誤。日期設在遠期以免受執行當日影響。
  it('資料完整時沒有錯誤', () => {
    const farMonday = nextMondayFromToday()
    expect(validateAppointment(validRequest({ date: farMonday }))).toEqual({})
  })

  // 情境：空白表單送出 → 每個必填欄位都要有對應的錯誤訊息，顯示在欄位下方。
  it('空白表單列出每個必填欄位的錯誤', () => {
    const errors = validateAppointment({})
    expect(Object.keys(errors).sort()).toEqual([
      'branchId',
      'consent',
      'date',
      'name',
      'phone',
      'serviceId',
      'slot',
    ])
  })

  // 情境：未勾選私隱同意 → 不可送出（個資收集須取得同意）。
  it('未同意私隱政策時提示', () => {
    expect(validateAppointment(validRequest({ consent: false })).consent).toBe('請先同意私隱政策')
  })

  // 情境：備註超過 1000 字 → 提示長度限制。
  it('備註過長時提示', () => {
    expect(validateAppointment(validRequest({ notes: '字'.repeat(1001) })).notes).toBeDefined()
  })
})

/** 取得今日之後的第一個星期一（香港時區），確保測試不受執行日期影響 */
function nextMondayFromToday(): string {
  const d = new Date(Date.now() + 8 * 3600_000)
  do d.setUTCDate(d.getUTCDate() + 1)
  while (d.getUTCDay() !== 1)
  return d.toISOString().slice(0, 10)
}
