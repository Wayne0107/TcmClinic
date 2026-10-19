// 公開內容頁（SSR 輸出的 HTML 與 payload）允許 CDN／反向代理短暫快取（docs/FRONTEND.md §9）。
// 不寫成 routeRules '/**'：Netlify 會把它輸出到 _headers 的 /*，蓋掉 /_nuxt/* 等靜態檔的長期快取，
// 而 _headers 本來就不套用到 SSR 回應。不用 Nitro 的 swr：其快取回應會略過 compression.ts，HTML 變成未壓縮。
const PAGE_CACHE_CONTROL = 'public, max-age=0, s-maxage=600, stale-while-revalidate=86400'

export default defineNitroPlugin((nitroApp) => {
  nitroApp.hooks.hook('render:response', (response, { event }) => {
    // 錯誤頁不快取，避免暫時性的 404／500 被 CDN 留住
    if (event.path.startsWith('/__nuxt_error') || (response.statusCode ?? 200) >= 400) return
    // routeRules 已指定的路徑（如 /booking 的 no-store）以其為準
    if (getResponseHeader(event, 'cache-control')) return
    const headers = (response.headers ??= {})
    headers['cache-control'] ??= PAGE_CACHE_CONTROL
  })
})
