import { defineConfig, mergeConfig } from 'vitest/config'
import viteConfig from './vite.config.ts'

export default mergeConfig(
  viteConfig,
  defineConfig({
    test: {
      environment: 'happy-dom',
      include: ['src/**/*.test.ts'],
      // Vuetify 以 ESM 原始碼發佈且會 import CSS，需交由 Vite 轉譯。
      server: { deps: { inline: ['vuetify'] } },
      coverage: {
        provider: 'v8',
        include: ['src/**/*.{ts,vue}'],
        exclude: ['src/**/*.test.ts', 'src/main.ts'],
      },
    },
  }),
)
