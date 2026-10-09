import { mount } from '@vue/test-utils'
import { describe, expect, it } from 'vitest'
import { createMemoryHistory } from 'vue-router'
import { createVuetify } from 'vuetify'
import App from '@/App.vue'
import { createAppRouter } from '.'

async function mountAt(path: string) {
  const router = createAppRouter(createMemoryHistory())
  await router.push(path)
  await router.isReady()
  return mount(App, { global: { plugins: [router, createVuetify()] } })
}

describe('後台路由', () => {
  // 情境：使用者輸入不存在的網址 → 應顯示找不到頁面並提供回首頁連結，而不是空白頁或首頁。
  it('未定義的路徑顯示找不到頁面', async () => {
    const wrapper = await mountAt('/no-such-page')

    expect(wrapper.find('h1').text()).toBe('找不到頁面')
    expect(wrapper.find('a[href="/"]').exists()).toBe(true)
  })

  // 情境：根路徑 → 應進入後台首頁，而不是被萬用路由攔成找不到頁面。
  it('根路徑顯示後台首頁', async () => {
    const wrapper = await mountAt('/')

    expect(wrapper.find('h1').text()).toBe('診所管理後台')
  })
})
