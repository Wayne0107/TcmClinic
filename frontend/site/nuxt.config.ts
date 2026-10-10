import tailwindcss from '@tailwindcss/vite'

// 後端位址：預設為後端 launchSettings 的 http 位址，可用環境變數覆寫（docs/FRONTEND.md §5.1）。
const apiTarget = process.env.API_PROXY_TARGET ?? 'http://localhost:5143'
// 雛形階段：後端 API 尚未完成，/api/** 由 server/api 的 mock 回應（假資料）。
// 後端完成後設 NUXT_USE_MOCK_API=false，/api/** 即改為代理到後端，頁面與 composable 不需修改呼叫路徑。
const useMockApi = process.env.NUXT_USE_MOCK_API !== 'false'
const siteUrl = process.env.NUXT_PUBLIC_SITE_URL ?? 'https://syttcm.com'

export default defineNuxtConfig({
  compatibilityDate: '2026-10-09',
  modules: ['@nuxt/eslint', '@nuxt/fonts'],
  // devtools 暫停使用：@nuxt/devtools 3.x 依賴有 Critical 弱點的 simple-git 3，修補版 4.x 與它不相容（DEF-024）。
  devtools: { enabled: false },
  devServer: { port: 3000 },
  css: ['~/assets/css/main.css'],
  vite: { plugins: [tailwindcss()] },

  app: {
    head: {
      // 語系：香港繁體中文（沿用舊站 zh-Hant-HK，docs/FRONTEND.md §12）
      htmlAttrs: { lang: 'zh-Hant-HK' },
      charset: 'utf-8',
      viewport: 'width=device-width, initial-scale=1, viewport-fit=cover',
      meta: [
        { name: 'theme-color', content: '#ffffff' },
        { name: 'format-detection', content: 'telephone=no' },
      ],
      link: [
        { rel: 'icon', type: 'image/png', sizes: '32x32', href: '/favicon-32.png' },
        { rel: 'apple-touch-icon', href: '/apple-touch-icon.png' },
        // 標題中文字型（思源宋體）非同步載入：@font-face 規則約 100 條，放進主 CSS 會阻擋首次繪製
        //（docs/FRONTEND.md §9「中文字型要做子集化或減少字重」：只載入 700 一個字重）。
        { rel: 'preconnect', href: 'https://fonts.googleapis.com' },
        { rel: 'preconnect', href: 'https://fonts.gstatic.com', crossorigin: '' },
        {
          rel: 'stylesheet',
          href: 'https://fonts.googleapis.com/css2?family=Noto+Serif+TC:wght@700&display=swap',
          media: 'print',
          onload: "this.media='all'",
        },
      ],
      noscript: [
        {
          innerHTML:
            '<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Noto+Serif+TC:wght@700&display=swap">',
        },
      ],
    },
  },

  fonts: {
    // 只自行託管英文小標（檔案很小）；中文標題字型見上方 head.link
    families: [
      { name: 'Cormorant Garamond', provider: 'google', weights: [500], styles: ['normal'] },
      { name: 'Noto Serif TC', provider: 'none' },
    ],
  },

  runtimeConfig: {
    // SSR 直接呼叫後端（伺服器端不能用相對路徑）；正式環境以 NUXT_API_BASE 覆寫。
    apiBase: apiTarget,
    public: { siteUrl, useMockApi },
  },

  routeRules: {
    // 瀏覽器端呼叫 /api/...，由 Nitro 代理到後端並去掉前綴（mock 模式下改由 server/api 回應）。
    '/api/**': {
      headers: { 'cache-control': 'no-store' },
      ...(useMockApi ? {} : { proxy: `${apiTarget}/**` }),
    },
    // 公開內容頁允許 CDN／反向代理短暫快取（docs/FRONTEND.md §9）。
    // 不用 Nitro 的 swr：其快取回應會略過 server/plugins/compression.ts，HTML 變成未壓縮。
    '/**': {
      headers: { 'cache-control': 'public, max-age=0, s-maxage=600, stale-while-revalidate=86400' },
    },
    '/booking': { headers: { 'cache-control': 'no-store' } },
    '/images/**': { headers: { 'cache-control': 'public, max-age=31536000, immutable' } },
    '/media/**': { headers: { 'cache-control': 'public, max-age=2592000' } },
  },

  nitro: {
    // 在 pnpm＋Windows 下，Nitro 會把 nuxt runtime 外部化成 .output/server/node_modules/nuxt，
    // 導致 nuxt/internal/precomputed 對到空的 stub，正式建置的每個頁面都回 500
    //（Either manifest or precomputed data must be provided）。強制打包 nuxt 以避免此問題（.memory MEM-2026-0005）。
    externals: { inline: ['nuxt'] },
    // 靜態資源預先壓縮（Brotli／gzip）；SSR HTML 由 server/plugins/compression.ts 壓縮
    compressPublicAssets: { gzip: true, brotli: true },
  },

  experimental: { payloadExtraction: true },
  typescript: {
    strict: true,
  },
})
