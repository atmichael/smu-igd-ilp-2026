import react from '@vitejs/plugin-react'
import basicSsl from '@vitejs/plugin-basic-ssl'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react(), basicSsl()],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5035',
        changeOrigin: true,
      },
    },
  },
})
