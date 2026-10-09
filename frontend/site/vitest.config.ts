import { defineVitestConfig } from '@nuxt/test-utils/config'

export default defineVitestConfig({
  test: {
    environment: 'nuxt',
    include: ['test/**/*.test.ts', 'app/**/*.test.ts'],
    coverage: {
      provider: 'v8',
      include: ['app/**/*.{ts,vue}'],
    },
  },
})
