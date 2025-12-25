/** @type {import('tailwindcss').Config} */
export default {
  // จุดสำคัญคือตรงนี้! บอกให้มันไปหา class ในไฟล์ html และ vue
  content: [
    "./index.html",
    "./src/**/*.{vue,js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', 'Kanit', 'sans-serif'],
        display: ['Kanit', 'sans-serif'],
      },
      colors: {
        mfec: {
          blue: '#005697', // สีน้ำเงิน MFEC
        }
      }
    },
  },
  plugins: [],
}
