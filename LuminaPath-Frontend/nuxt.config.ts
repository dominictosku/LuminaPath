// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  ssr: false,
  runtimeConfig: {
    public: {
      API_ENDPOINT: process.env.NUXT_API_ENDPOINT,
    },
  },
  css: [
      '@ionic/core/css/core.css',
      '@ionic/core/css/normalize.css',
      '@ionic/core/css/structure.css',
      '@ionic/core/css/typography.css',
      '@ionic/core/css/ionic.bundle.css',
      '~/theme/variables.css',
      '~/assets/css/customTailwind.css'
  ],
  alias: {
    pinia: "/node_modules/@pinia/nuxt/node_modules/pinia/dist/pinia.mjs"
  },
  modules: [
    '@nuxtjs/tailwindcss',
    '@nuxtjs/ionic',
    '@pinia/nuxt',
    '@formkit/nuxt',
    'nuxt-swiper',
    'nuxt-icons',
    'nuxt3-leaflet'
  ],
  pinia: {
    autoImports: [
      // automatically imports `defineStore`
      'defineStore', // import { defineStore } from 'pinia'
      ['defineStore', 'definePiniaStore'], // import { defineStore as definePiniaStore } from 'pinia'
    ],
  },
  devtools: { enabled: true }
})
