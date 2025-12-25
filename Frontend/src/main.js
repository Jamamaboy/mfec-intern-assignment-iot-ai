import { createApp } from 'vue'
import App from './App.vue'
import Tres from '@tresjs/core'
import './assets/main.css'

const app = createApp(App)
app.use(Tres)
app.mount('#app')
