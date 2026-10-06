/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        cafe: {
          orange: {
            DEFAULT: '#EA580C',
            light: '#F97316',
            dark: '#C2410C',
            subtle: '#FFF7ED',
          },
          black: {
            DEFAULT: '#0A0A0A',
            surface: '#121212',
            card: '#18181B',
            muted: '#27272A',
          },
          white: '#FFFFFF',
          border: {
            light: '#E4E4E7',
            dark: '#27272A',
          }
        }
      },
      fontFamily: {
        sans: [
          'Inter',
          '-apple-system',
          'BlinkMacSystemFont',
          'Segoe UI',
          'Roboto',
          'sans-serif',
        ],
      },
    },
  },
  plugins: [],
}
