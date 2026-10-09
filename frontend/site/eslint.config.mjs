// @tcm-clinic/site 的 ESLint 設定（docs/FRONTEND.md §3、§4）；基礎設定由 @nuxt/eslint 模組產生。
import eslintConfigPrettier from 'eslint-config-prettier/flat'
import withNuxt from './.nuxt/eslint.config.mjs'

export default withNuxt(
  {
    rules: {
      '@typescript-eslint/no-explicit-any': 'error',
      'vue/no-v-html': 'error',
      'no-console': 'error',
      'no-debugger': 'error',
    },
  },
  {
    // 分層：頁面與元件不得直接呼叫 API，一律經由 composable（docs/FRONTEND.md §3）。
    files: ['app/pages/**/*.vue', 'app/components/**/*.vue', 'app/layouts/**/*.vue'],
    rules: {
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            {
              group: ['openapi-fetch'],
              message: '頁面與元件不得直接呼叫 API，請經由 composable（docs/FRONTEND.md §3）。',
            },
          ],
        },
      ],
      'no-restricted-syntax': [
        'error',
        {
          selector:
            "CallExpression[callee.name=/^([$]fetch|useFetch|useLazyFetch)$/], MemberExpression[property.name='$api']",
          message: '頁面與元件不得直接呼叫 API，請經由 composable（docs/FRONTEND.md §3）。',
        },
      ],
    },
  },
  eslintConfigPrettier,
)
