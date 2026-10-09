import { mountSuspended } from '@nuxt/test-utils/runtime'
import { describe, expect, it } from 'vitest'
import ErrorPage from '~/error.vue'

function errorOf(statusCode: number) {
  return createError({ statusCode })
}

describe('錯誤頁', () => {
  // 情境：瀏覽不存在的網址（404）→ 應明確告知找不到頁面，而不是通用錯誤訊息。
  it('404 時顯示找不到頁面', async () => {
    const wrapper = await mountSuspended(ErrorPage, { props: { error: errorOf(404) } })

    expect(wrapper.find('h1').text()).toBe('找不到頁面')
  })

  // 情境：伺服器錯誤（500）→ 顯示通用錯誤訊息，不洩漏錯誤細節（docs/FRONTEND.md §5.3）。
  it('其他錯誤顯示系統發生錯誤且不含錯誤細節', async () => {
    const error = createError({ statusCode: 500, message: 'SqlException: timeout' })
    const wrapper = await mountSuspended(ErrorPage, { props: { error } })

    expect(wrapper.find('h1').text()).toBe('系統發生錯誤')
    expect(wrapper.text()).not.toContain('SqlException')
  })
})
