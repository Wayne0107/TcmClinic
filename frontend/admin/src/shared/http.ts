import type { paths } from '@tcm-clinic/shared'
import createClient from 'openapi-fetch'

/**
 * 後台唯一的 API client：路徑、參數、body 與回應都受後端 OpenAPI 型別檢查（docs/FRONTEND.md §5.2）。
 * 以 /api 經 dev server 代理到後端。Bearer token 與 401 換新的 middleware 隨登入規格加入（§6.2）。
 */
export const http = createClient<paths>({ baseUrl: '/api' })
