// @tcm-clinic/shared 的 ESLint 設定（docs/FRONTEND.md §3、§4）。
import eslintConfigPrettier from 'eslint-config-prettier/flat'
import tseslint from 'typescript-eslint'

export default tseslint.config(
  { ignores: ['src/api/schema.d.ts'] },
  ...tseslint.configs.recommended,
  {
    rules: {
      '@typescript-eslint/no-explicit-any': 'error',
      'no-console': 'error',
      'no-debugger': 'error',
      // 共用套件比照後端 Core：不依賴任何前端框架或瀏覽器專屬 API（docs/FRONTEND.md §3）。
      'no-restricted-imports': [
        'error',
        {
          patterns: [
            {
              group: ['vue', 'vue-*', 'nuxt', 'nuxt/*', '#*', 'vuetify', 'vuetify/*', 'pinia'],
              message: '@tcm-clinic/shared 不得依賴前端框架（docs/FRONTEND.md §3）。',
            },
          ],
        },
      ],
    },
  },
  eslintConfigPrettier,
)
