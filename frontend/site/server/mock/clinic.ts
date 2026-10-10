import type { ClinicInfo } from '#shared/types/api'

// 文字一律取自舊站 syttcm.com 原文（頁尾、聯絡我們頁）。
export const clinic: ClinicInfo = {
  name: '聖儀堂中醫診所',
  nameEn: 'SHENG YI TANG TCM',
  description:
    '由註冊中醫潘巧儀醫師主理，專業中醫婦科及全科，兼理中醫美容與減肥修身。觀塘裕民中心・藍田匯景商場，以辨證論治守護您的身心健康。',
  phone: '5512 3132',
  phoneE164: '+85255123132',
  whatsapp: '85255123132',
  whatsappDefaultText: '您好，我想預約聖儀堂中醫診所。',
  hours: [
    { days: [1, 2, 3, 4, 5, 6], open: '10:30', close: '13:30' },
    { days: [1, 2, 3, 4, 5, 6], open: '14:30', close: '19:30' },
  ],
  hoursText: '星期一至六：上午 10:30–13:30｜下午 14:30–19:30',
  hoursNote: '實際時間以各分店公布為準，公眾假期或有調整。',
  closedNote: '星期日全日休診',
  branches: [
    {
      id: 1,
      slug: 'kwun-tong',
      name: '觀塘裕民中心',
      shortName: '觀塘',
      label: '觀塘店',
      address: '觀塘牛頭角道300-302號裕民中心商場地下23B',
      district: '觀塘',
      transport: '港鐵觀塘站 B1 出口，步行約 5 分鐘',
      mapQuery: '觀塘牛頭角道300-302號裕民中心',
      image: { src: 'entrance-door-1', alt: '聖儀堂中醫診所大門入口' },
      whatsappText: '您好，我想預約觀塘裕民中心診症',
    },
    {
      id: 2,
      slug: 'lam-tin',
      name: '藍田匯景商場',
      shortName: '藍田',
      label: '藍田店',
      address: '藍田匯景商場',
      district: '藍田',
      transport: '港鐵藍田站，連接匯景商場',
      mapQuery: '藍田匯景廣場',
      image: { src: 'entrance-door-2', alt: '聖儀堂診所門面' },
      whatsappText: '您好，我想預約藍田匯景商場診症',
    },
  ],
  disclaimer:
    '免責聲明：本網站所載中醫藥資訊僅供健康教育及一般參考之用，並不構成醫療診斷、治療建議或療效保證，亦不能取代註冊中醫師或註冊醫生的面診。療程效果因個人體質、病情及依從性而異。孕婦、哺乳期婦女、慢性病患者及正服用處方藥物者，應於接受任何療程前諮詢註冊中醫師。',
  copyright:
    '© 2026 聖儀堂中醫診所 SHENG YI TANG TCM. 版權所有。｜觀塘中醫・藍田中醫・中醫婦科・中醫美容・減肥修身',
}
