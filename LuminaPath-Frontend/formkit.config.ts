import { defineFormKitConfig } from '@formkit/vue'
import { generateClasses } from '@formkit/themes'
import { genesisIcons } from '@formkit/icons'
import myTailwindTheme from './tailwind-theme'

export default defineFormKitConfig(() => {
  // here we can access `useRuntimeConfig` because
  // our function will be called by Nuxt.

  return {
    icons: {
        ...genesisIcons,
      },
      config: {
        classes: generateClasses(myTailwindTheme),
      },
  }
})