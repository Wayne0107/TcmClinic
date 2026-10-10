// SSR 頁面 HTML 壓縮（Brotli 優先，其次 gzip）。
// 正式環境若由 IIS／nginx 反向代理壓縮，可移除此 plugin。靜態資源由 nitro.compressPublicAssets 預先壓縮。
import { brotliCompressSync, constants, gzipSync } from 'node:zlib'

export default defineNitroPlugin((nitroApp) => {
  nitroApp.hooks.hook('render:response', (response, { event }) => {
    if (typeof response.body !== 'string' || response.body.length < 1024) return
    // 錯誤頁（404 等）由 Nuxt 以內部路徑 /__nuxt_error 渲染，再由錯誤處理流程以文字寫出；
    // 先壓縮會把二進位內容當文字輸出，造成「標示 gzip 但內容損壞」，故錯誤頁不壓縮
    if (event.path.startsWith('/__nuxt_error') || (response.statusCode ?? 200) >= 400) return
    const contentType = String(response.headers?.['content-type'] ?? 'text/html')
    if (!contentType.includes('html')) return

    const accept = getRequestHeader(event, 'accept-encoding') ?? ''
    const headers = (response.headers ??= {})
    headers.vary = 'Accept-Encoding'
    if (accept.includes('br')) {
      response.body = brotliCompressSync(response.body, {
        params: { [constants.BROTLI_PARAM_QUALITY]: 5 },
      }) as unknown as string
      headers['content-encoding'] = 'br'
    } else if (accept.includes('gzip')) {
      response.body = gzipSync(response.body, { level: 6 }) as unknown as string
      headers['content-encoding'] = 'gzip'
    }
  })
})
