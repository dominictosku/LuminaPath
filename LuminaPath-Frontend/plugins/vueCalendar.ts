import { setupCalendar } from 'v-calendar';

export default defineNuxtPlugin((nuxtApp) => {
    nuxtApp.vueApp.use(setupCalendar, {})
  })