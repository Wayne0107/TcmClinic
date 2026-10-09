// 後端位址：預設為後端 launchSettings 的 http 位址，可用環境變數覆寫（docs/FRONTEND.md §5.1）。
const apiTarget = process.env.API_PROXY_TARGET ?? 'http://localhost:5143'

export default defineNuxtConfig({
  compatibilityDate: '2026-10-09',
  modules: ['@nuxt/eslint'],
  // devtools 暫停使用：@nuxt/devtools 3.x 依賴有 Critical 弱點的 simple-git 3，修補版 4.x 與它不相容（DEF-024）。
  devtools: { enabled: false },
  devServer: { port: 3000 },
  app: {
    head: {
      // 地區碼（如 zh-Hant-HK、zh-Hant-TW）依前台規格決定（docs/FRONTEND.md §12）。
      htmlAttrs: { lang: 'zh-Hant' },
    },
  },
  runtimeConfig: {
    // SSR 直接呼叫後端（伺服器端不能用相對路徑）；正式環境以 NUXT_API_BASE 覆寫。
    apiBase: apiTarget,
  },
  routeRules: {
    // 瀏覽器端呼叫 /api/...，由 Nitro 代理到後端並去掉前綴。
    '/api/**': { proxy: `${apiTarget}/**` },
  },
  nitro: {
    // 在 pnpm＋Windows 下，Nitro 會把 nuxt runtime 外部化成 .output/server/node_modules/nuxt，
    // 導致 nuxt/internal/precomputed 對到空的 stub，正式建置的每個頁面都回 500
    //（Either manifest or precomputed data must be provided）。強制打包 nuxt 以避免此問題（.memory MEM-2026-0005）。
    externals: { inline: ['nuxt'] },
  },
  typescript: {
    strict: true,
  },
})
