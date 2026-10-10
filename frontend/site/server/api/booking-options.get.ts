import { bookingServices } from '../mock/services/overview'
import { clinic } from '../mock/clinic'

// 預約表單選項：與舊站表單相同（分店、診療項目）
export default defineEventHandler(() => ({
  services: bookingServices,
  branches: clinic.branches.map((b) => ({ id: b.id, name: b.name })),
}))
