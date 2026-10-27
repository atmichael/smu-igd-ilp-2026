import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

const backendTarget = process.env.services__backend__https__0 
   || process.env.services__backend__http__0 
   || 'http://localhost:5000';

export default defineConfig({
  plugins: [react()],
  server: {
    port: parseInt(process.env.PORT || '5173'),
    allowedHosts: [
      'ilpteam5.ngrok.dev'
    ],
    proxy: {
      '/api': {
        target: backendTarget,
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
