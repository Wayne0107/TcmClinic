import type { paths } from '@tcm-clinic/shared'
import createClient from 'openapi-fetch'

/**
 * 前台的 API client，以 `useNuxtApp().$api` 取用（docs/FRONTEND.md §5.2）。
 * SSR 時直接呼叫後端；瀏覽器端走 /api 代理（routeRules）。
 */
export default defineNuxtPlugin(() => {
  const baseUrl = import.meta.server ? useRuntimeConfig().apiBase : '/api'
  return { provide: { api: createClient<paths>({ baseUrl }) } }
})
