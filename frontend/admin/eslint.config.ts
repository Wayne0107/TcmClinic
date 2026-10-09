// @tcm-clinic/admin 的 ESLint 設定（docs/FRONTEND.md §3、§4）。
import { defineConfigWithVueTs, vueTsConfigs } from '@vue/eslint-config-typescript'
import eslintConfigPrettier from 'eslint-config-prettier/flat'
import pluginVue from 'eslint-plugin-vue'

// 分層限制（docs/FRONTEND.md §3）。flat config 中同一條規則以最後一筆設定為準，
// 所以各檔案範圍要把適用的限制組合在一起，不能分開設定。
const noApiInComponents = {
  group: ['openapi-fetch', '@/shared/http', '**/api'],
  message: '元件不得直接呼叫 API，請經由 composable 或 store（docs/FRONTEND.md §3）。',
}
const noCrossFeature = {
  group: ['@/features/**', '../../*'],
  message: '功能之間不得互相引用，共用的請移到 src/shared/（docs/FRONTEND.md §3）。',
}
const noRawClient = {
  group: ['openapi-fetch'],
  message: '請使用 src/shared/http.ts 的 client（docs/FRONTEND.md §5.2）。',
}

export default defineConfigWithVueTs(
  { ignores: ['dist/**', 'coverage/**'] },
  pluginVue.configs['flat/recommended'],
  vueTsConfigs.recommended,
  {
    rules: {
      '@typescript-eslint/no-explicit-any': 'error',
      'vue/no-v-html': 'error',
      'no-console': 'error',
      'no-debugger': 'error',
    },
  },
  {
    files: ['src/**/*.vue'],
    rules: { 'no-restricted-imports': ['error', { patterns: [noApiInComponents] }] },
  },
  {
    files: ['src/features/**/*.ts'],
    rules: { 'no-restricted-imports': ['error', { patterns: [noCrossFeature, noRawClient] }] },
  },
  {
    files: ['src/features/**/*.vue'],
    rules: {
      'no-restricted-imports': ['error', { patterns: [noApiInComponents, noCrossFeature] }],
    },
  },
  eslintConfigPrettier,
)
