// 舊網址錨點導向新頁（對照表見 app/utils/legacy-anchors.ts）。錨點只存在於瀏覽器端，伺服器端略過。
export default defineNuxtRouteMiddleware((to) => {
  if (import.meta.server || !to.hash) return
  const target = legacyAnchorTarget(to.path, to.hash)
  if (target) return navigateTo(target, { replace: true })
})
