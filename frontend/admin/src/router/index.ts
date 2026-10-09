import { createRouter, createWebHistory, type RouterHistory, type RouteRecordRaw } from 'vue-router'

/** 後台路由；路由元件一律動態載入（docs/FRONTEND.md §9）。 */
export const routes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'home',
    component: () => import('@/features/home/views/HomeView.vue'),
  },
  {
    // 其餘未定義的路徑一律顯示找不到頁面，不導回首頁（避免使用者以為網址有效）。
    path: '/:pathMatch(.*)*',
    name: 'not-found',
    component: () => import('@/shared/views/NotFoundView.vue'),
  },
]

/** 建立路由；測試可傳入 memory history。 */
export function createAppRouter(
  history: RouterHistory = createWebHistory(import.meta.env.BASE_URL),
) {
  return createRouter({ history, routes })
}
