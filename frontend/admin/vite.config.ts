import { fileURLToPath, URL } from 'node:url'
import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vite'
import vuetify from 'vite-plugin-vuetify'

// 後端位址：預設為後端 launchSettings 的 http 位址，可用環境變數覆寫（docs/FRONTEND.md §5.1）。
const apiTarget = process.env.API_PROXY_TARGET ?? 'http://localhost:5143'

export default defineConfig({
  plugins: [vue(), vuetify({ autoImport: true })],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5173,
    strictPort: true,
    proxy: {
      // 前端一律呼叫 /api/...，代理時去掉前綴；同源代理讓 refresh token 的 HttpOnly Cookie 自動帶上。
      '/api': {
        target: apiTarget,
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/api/, ''),
      },
    },
  },
})
