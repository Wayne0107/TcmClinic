import type { Offer } from '#shared/types/api'

// 優惠：逐字取自舊站 /services 收費一覽（服務項目／內容說明／參考收費）。舊站沒有其他優惠文案。
export const offers: Offer[] = [
  {
    id: 'facial',
    title: '面部經絡理療',
    description: '面部肌肉筋膜按摩、淋巴排毒、面部清潔、中藥美白倒膜，連中醫診症及調理中藥一日',
    price: '$380（試做價）',
    serviceSlug: 'beauty',
    bookingServiceId: 5,
  },
  {
    id: 'acupuncture-slim',
    title: '針灸瘦身（單次）',
    description: '針灸減肥，連中醫診症及中藥一包',
    price: '$398',
    serviceSlug: 'slim',
    bookingServiceId: 7,
  },
  {
    id: 'acupuncture-slim-5',
    title: '針灸瘦身療程',
    description: '5 次療程套裝，平均每次約 $376',
    price: '$1880',
    serviceSlug: 'slim',
    bookingServiceId: 7,
  },
  {
    id: 'womb-warming',
    title: '腹部暖宮（單次）',
    description: '溫經散寒調理，連中醫診症及中藥一包',
    price: '$350',
    serviceSlug: 'slim',
    bookingServiceId: 8,
  },
]

/** 收費一覽的附註（舊站原文） */
export const offerNotes = [
  '＊以上收費僅供參考，個別療程因應病情及用藥而異，詳情請致電 5512 3132 查詢。',
  '試做價及療程套裝屬期間性安排，最終收費以診所現行報價為準，建議預約時一併確認。本網站列出的價格僅供參考，不構成任何療效保證。',
]
