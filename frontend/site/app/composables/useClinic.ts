import type { ClinicInfo } from '#shared/types/api'

/** 診所共用資料：全站只請求一次（同 key 共用快取） */
export function useClinic() {
  return useFetch<ClinicInfo>('/api/clinic', {
    key: 'clinic',
    getCachedData: (key, nuxtApp) => nuxtApp.payload.data[key] ?? nuxtApp.static.data[key],
  })
}
