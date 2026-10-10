import { mountSuspended } from '@nuxt/test-utils/runtime'
import { describe, expect, it } from 'vitest'
import RichText from '~/components/RichText.vue'
import { formatDate, formatDays, whatsappUrl } from '~/utils/format'
import { plainText } from '~/composables/useJsonLd'

describe('RichText', () => {
  // 情境：內容含粗體與站內連結 → 轉成 strong 與站內連結，且不以 v-html 輸出。
  it('轉換粗體與站內連結', async () => {
    const wrapper = await mountSuspended(RichText, {
      props: { text: '先看**月經記錄**，再[預約](/booking)。', tag: 'p' },
    })
    expect(wrapper.find('strong').text()).toBe('月經記錄')
    expect(wrapper.find('a').attributes('href')).toBe('/booking')
    expect(wrapper.text()).toBe('先看月經記錄，再預約。')
  })

  // 情境：外部連結 → 新視窗開啟並加 rel="noopener noreferrer"（docs/FRONTEND.md §7）。
  it('外部連結加上 noopener noreferrer', async () => {
    const wrapper = await mountSuspended(RichText, {
      props: { text: '[衛生署](https://www.dh.gov.hk)' },
    })
    const a = wrapper.find('a')
    expect(a.attributes('target')).toBe('_blank')
    expect(a.attributes('rel')).toBe('noopener noreferrer')
  })

  // 情境：連結是 javascript: → 不輸出成連結，只留文字（避免後台輸入的內容造成 XSS）。
  it('不在白名單的連結只顯示文字', async () => {
    const wrapper = await mountSuspended(RichText, {
      props: { text: '請[點此](javascript:alert%281%29)查看' },
    })
    expect(wrapper.find('a').exists()).toBe(false)
    expect(wrapper.text()).toBe('請點此查看')
  })

  // 情境：文字含 HTML 標籤 → 當作純文字顯示，不可被解析成元素。
  it('不解析文字中的 HTML', async () => {
    const wrapper = await mountSuspended(RichText, {
      props: { text: '<img src=x onerror=alert(1)>' },
    })
    expect(wrapper.find('img').exists()).toBe(false)
  })
})

describe('format', () => {
  // 情境：ISO 時間 → 顯示為「2026年10月9日」。
  it('formatDate 顯示中文日期', () => {
    expect(formatDate('2026-10-09T14:13:00+08:00')).toBe('2026年10月9日')
  })

  // 情境：連續應診日合併顯示，不連續則逐日列出。
  it('formatDays 合併連續星期', () => {
    expect(formatDays([1, 2, 3, 4, 5, 6])).toBe('星期一至六')
    expect(formatDays([1, 3, 5])).toBe('星期一、星期三、星期五')
  })

  // 情境：WhatsApp 預填訊息含中文 → 必須 URL 編碼（舊站缺陷 F6）。
  it('whatsappUrl 編碼預填訊息', () => {
    expect(whatsappUrl('85255123132', '您好')).toBe(
      'https://wa.me/85255123132?text=%E6%82%A8%E5%A5%BD',
    )
  })

  // 情境：結構化資料不能含 Markdown 語法 → 轉為純文字。
  it('plainText 移除 RichText 語法', () => {
    expect(plainText('請看**重點**及[收費](/services#price)')).toBe('請看重點及收費')
  })
})
