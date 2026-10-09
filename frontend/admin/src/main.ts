import { createPinia } from 'pinia'
import { createApp } from 'vue'
import App from './App.vue'
import { vuetify } from './plugins/vuetify'
import { createAppRouter } from './router'

createApp(App).use(createPinia()).use(createAppRouter()).use(vuetify).mount('#app')
