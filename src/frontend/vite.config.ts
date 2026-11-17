import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from 'tailwindcss';
import autoprefixer from 'autoprefixer';

export default defineConfig({
  plugins: [react()],
  css: {
    postcss: {
      plugins: [tailwindcss(), autoprefixer()],
    },
  },
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          vendor: ['react', 'react-dom', 'react-router-dom'],
          ui: ['lucide-react', 'react-hook-form', '@hookform/resolvers', 'zod'],
          state: ['zustand'],
          query: ['@tanstack/react-query'],
          utils: ['clsx', 'tailwind-merge', 'qrcode'],
        },
      },
    },
    chunkSizeWarningLimit: 300,
  },
  server: {
    host: true,
    port: 3000,
    allowedHosts: true,
    proxy: {
      '/api': {
        target: 'http://localhost:7890',
        changeOrigin: true,
      },
    },
  },
});
