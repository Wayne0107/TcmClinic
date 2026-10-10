import type {
  AboutContent,
  AppointmentRequest,
  AppointmentResponse,
  BookingServiceOption,
  Doctor,
  HomeContent,
  Offer,
  Paged,
  Post,
  PostSummary,
  ServiceDetail,
  ServicesOverview,
} from '#shared/types/api'

/*
 * 前台資料存取（docs/FRONTEND.md §3：頁面與元件不得直接呼叫 API，一律經由 composable）。
 *
 * 雛形階段：後端 API 尚未完成，/api/** 由 server/api 的 mock 回應（內容取自舊站原文，nuxt.config 的 useMockApi）。
 * 後端完成後：改以 `useAsyncData(key, () => useNuxtApp().$api.GET(...))` 呼叫型別化 client（§5.2），
 * 回應型別改取 @tcm-clinic/shared 產生的型別；頁面與元件不需修改。
 */

export function useHomeContent() {
  return useFetch<HomeContent>('/api/pages/home', { key: 'page-home' })
}

export function useAboutContent() {
  return useFetch<AboutContent>('/api/pages/about', { key: 'page-about' })
}

export function useDoctors() {
  return useFetch<Doctor[]>('/api/doctors', { key: 'doctors' })
}

export function useDoctor(slug: string) {
  return useFetch<Doctor>(`/api/doctors/${encodeURIComponent(slug)}`, { key: `doctor-${slug}` })
}

export function useServicesOverview() {
  return useFetch<ServicesOverview>('/api/services', { key: 'services' })
}

export function useServiceDetail(slug: ServiceDetail['slug']) {
  return useFetch<ServiceDetail>(`/api/services/${encodeURIComponent(slug)}`, {
    key: `service-${slug}`,
  })
}

export interface PostQuery {
  page?: number
  pageSize?: number
  exclude?: string
}

export function usePosts(query: PostQuery) {
  const key = `posts-${query.exclude ?? ''}-${query.page ?? 1}-${query.pageSize ?? ''}`
  return useFetch<Paged<PostSummary>>('/api/posts', { key, query })
}

export function usePost(slug: string) {
  return useFetch<Post>(`/api/posts/${encodeURIComponent(slug)}`, { key: `post-${slug}` })
}

export function useOffers() {
  return useFetch<{ items: Offer[]; notes: string[] }>('/api/offers', { key: 'offers' })
}

export interface BookingOptions {
  services: BookingServiceOption[]
  branches: { id: number; name: string }[]
}

export function useBookingOptions() {
  return useFetch<BookingOptions>('/api/booking-options', { key: 'booking-options' })
}

/**
 * 送出預約申請（同舊站：POST 後顯示伺服器回傳的訊息）。
 * 後端回 400／422 時把欄位錯誤原樣回傳給表單（docs/FRONTEND.md §5.3）；網路錯誤回傳舊站相同的提示，不暴露例外細節。
 */
export async function submitAppointment(body: AppointmentRequest): Promise<AppointmentResponse> {
  try {
    return await $fetch<AppointmentResponse>('/api/appointments', { method: 'POST', body })
  } catch (e: unknown) {
    const data = (e as { data?: AppointmentResponse }).data
    if (data?.message) return { ok: false, message: data.message, errors: data.errors }
    return { ok: false, message: '網絡連線失敗，請使用 WhatsApp 或電話聯絡我們。' }
  }
}
