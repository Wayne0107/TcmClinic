import type { AppointmentRequest, AppointmentResponse } from '#shared/types/api'
import { normalizeHkPhone, validateAppointment } from '#shared/utils/booking'

// mock：只存在記憶體，重啟即消失。正式版由 .NET 後端寫入資料庫並通知診所。
// 訊息文字：舊站的伺服器回應文字不可見，成功訊息沿用舊站表單下方的原文提示；錯誤訊息屬 mock，正式版以後端回應為準。
const received: (AppointmentRequest & { reference: string; receivedAt: string })[] = []

export default defineEventHandler(async (event): Promise<AppointmentResponse> => {
  const body = await readBody<Partial<AppointmentRequest>>(event)

  // 蜜罐有值：當作成功回應，但不處理
  if (body?.website)
    return { ok: true, message: '送出後診所會以電話或 WhatsApp 聯絡確認，提交並不代表預約已確定。' }

  const errors = validateAppointment(body ?? {})
  if (Object.keys(errors).length) {
    setResponseStatus(event, 422)
    return { ok: false, message: '請檢查標示的欄位。', errors }
  }

  const reference = `SYT-${body.date!.replaceAll('-', '').slice(2)}-${String(received.length + 1).padStart(3, '0')}`
  received.push({
    ...(body as AppointmentRequest),
    phone: normalizeHkPhone(body.phone!)!,
    reference,
    receivedAt: new Date().toISOString(),
  })

  return {
    ok: true,
    reference,
    message: '送出後診所會以電話或 WhatsApp 聯絡確認，提交並不代表預約已確定。',
  }
})
