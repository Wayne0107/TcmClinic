import 'vuetify/styles'
import { createVuetify } from 'vuetify'
import { aliases, mdi } from 'vuetify/iconsets/mdi-svg'

/** 後台的 Vuetify 實例；元件由 vite-plugin-vuetify 按需載入，圖示用 @mdi/js 的 SVG（docs/FRONTEND.md §9）。 */
export const vuetify = createVuetify({
  icons: {
    defaultSet: 'mdi',
    aliases,
    sets: { mdi },
  },
})
