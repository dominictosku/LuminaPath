import type { Config } from 'tailwindcss'
import defaultTheme from 'tailwindcss/defaultTheme'
import FormKitVariants  from '@formkit/themes/tailwindcss'

export default <Partial<Config>>{
    content: [
        './tailwind-theme.ts',
      ],
      plugins: [FormKitVariants],
}