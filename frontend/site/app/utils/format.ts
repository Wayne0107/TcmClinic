export const WEEKDAYS = [
  '星期日',
  '星期一',
  '星期二',
  '星期三',
  '星期四',
  '星期五',
  '星期六',
] as const
export const WEEKDAYS_SHORT = ['日', '一', '二', '三', '四', '五', '六'] as const

/** 2026-10-09T14:13:00+08:00 → 2026年10月9日 */
export function formatDate(iso: string): string {
  const [y, m, d] = iso.slice(0, 10).split('-').map(Number)
  return `${y}年${m}月${d}日`
}

export function telUrl(e164: string) {
  return `tel:${e164}`
}

export function whatsappUrl(number: string, text?: string) {
  return `https://wa.me/${number}${text ? `?text=${encodeURIComponent(text)}` : ''}`
}

export function mapUrl(query: string) {
  return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(query)}`
}

/** 把連續星期合併成「星期一至六」 */
export function formatDays(days: number[]): string {
  const sorted = [...days].sort()
  const contiguous = sorted.every((d, i) => i === 0 || d === sorted[i - 1]! + 1)
  if (contiguous && sorted.length > 2)
    return `星期${WEEKDAYS_SHORT[sorted[0]!]}至${WEEKDAYS_SHORT[sorted.at(-1)!]}`
  return sorted.map((d) => WEEKDAYS[d]).join('、')
}
