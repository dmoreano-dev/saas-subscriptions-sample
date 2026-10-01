import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Aspire injects the API address as services__api__http__0 (service discovery).
// When Vite runs on its own, fall back to the API's default launchSettings URL.
const apiTarget = process.env.services__api__http__0 ?? 'http://localhost:5282'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // Local equivalent of the hosted Vercel /api rewrite (docs/plan.md §10).
    proxy: {
      '/api': { target: apiTarget, changeOrigin: true },
    },
  },
})
