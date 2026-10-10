import { mountSuspended, registerEndpoint } from '@nuxt/test-utils/runtime'
import { describe, expect, it } from 'vitest'
import { NOTES_MAX_LENGTH } from '#shared/utils/booking'
import BookingForm from '~/components/BookingForm.vue'

// MOCK: 後端預約 API 尚未完成，於網路邊界以 registerEndpoint 模擬（docs/FRONTEND.md §8.2）
registerEndpoint('/api/booking-options', () => ({
  services: [{ id: 5, name: '面部經絡理療／中藥面膜' }],
  branches: [
    { id: 1, name: '觀塘裕民中心' },
    { id: 2, name: '藍田匯景商場' },
  ],
}))

describe('BookingForm', () => {
  // 情境：表單欄位與舊站相同，且必填欄位交由瀏覽器原生驗證（同舊站）→ 每個必填欄位都要有 required。
  it('欄位與舊站相同且必填欄位設為 required', async () => {
    const wrapper = await mountSuspended(BookingForm, { props: { sourcePage: 'test' } })
    const labels = wrapper.findAll('label').map((l) => l.text())
    for (const text of [
      '姓名 *',
      '聯絡電話 *',
      '選擇分店 *',
      '預約日期 *',
      '預約時段 *',
      '診療項目 *',
    ]) {
      expect(labels).toContain(text)
    }
    for (const id of ['bk-name', 'bk-phone', 'bk-branch', 'bk-date', 'bk-slot', 'bk-service']) {
      expect(wrapper.find(`#${id}`).attributes('required')).toBeDefined()
    }
    expect(wrapper.find('input[type="checkbox"]').attributes('required')).toBeDefined()
    expect(wrapper.find('button[type="submit"]').text()).toBe('確認提交預約')
  })

  // 情境：備註輸入上限須與送出驗證一致 → 否則可輸入的字數送出後會被拒。
  it('備註 maxlength 與驗證上限一致', async () => {
    const wrapper = await mountSuspended(BookingForm, { props: { sourcePage: 'test' } })
    expect(wrapper.find('textarea').attributes('maxlength')).toBe(String(NOTES_MAX_LENGTH))
  })

  // 情境：由服務頁的預約連結進入（?service=5）→ 診療項目應已預先選好。
  it('依網址參數預選診療項目', async () => {
    const wrapper = await mountSuspended(BookingForm, {
      props: { sourcePage: 'test' },
      route: '/booking?service=5',
    })
    const select = wrapper.find<HTMLSelectElement>('#bk-service')
    expect(select.element.value).toBe('5')
  })
})
