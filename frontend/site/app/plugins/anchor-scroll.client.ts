/**
 * 錨點跳轉前先解除延後繪製（.cv-auto 的 content-visibility）。
 * 未繪製的區塊以預估高度佔位，直接跳到 #id 會停在錯的位置；
 * 帶 hash 進站、點同頁錨點或路由到 #id 時，整頁改為正常繪製，讓瀏覽器與路由算出正確落點。
 */
export default defineNuxtPlugin(() => {
  const root = document.documentElement
  const renderAll = () => root.classList.add('cv-off')

  if (location.hash) renderAll()

  // 捕獲階段：在瀏覽器預設的錨點捲動之前執行
  document.addEventListener(
    'click',
    (e) => {
      const link = (e.target as Element | null)?.closest?.('a[href*="#"]')
      if (link) renderAll()
    },
    { capture: true },
  )

  useRouter().beforeEach((to) => {
    if (to.hash) renderAll()
  })
})
