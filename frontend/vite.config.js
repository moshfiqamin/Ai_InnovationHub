// ============================================================
// FILE   : vite.config.js
// LAYER  : Frontend build configuration
// PURPOSE: Dev server config. The /api proxy forwards frontend
//          requests to the ASP.NET Core backend so the browser
//          never hits a CORS wall during development.
// ============================================================
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      // Any request starting with /api goes to the backend on :5099
      '/api': {
        target: 'http://localhost:5099',
        changeOrigin: true,
      },
    },
  },
})
