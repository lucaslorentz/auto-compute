import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import basicSsl from '@vitejs/plugin-basic-ssl';

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const basePath = env.EXPLORER_BASE || '/auto-compute-explorer/';
  const proxyTarget = env.VITE_EXPLORER_PROXY_TARGET;

  console.log(`[Vite Explorer] Mode: ${mode}`);
  console.log(`[Vite Explorer] Base Path: ${basePath}`);
  console.log(`[Vite Explorer] Proxy Target: ${proxyTarget || 'NONE'}`);

  return {
    plugins: [react(), basicSsl()],
    base: mode === 'development' ? basePath : './',
    server: {
      port: 5173,
      strictPort: true,
      proxy: proxyTarget ? {
        '/': {
          target: proxyTarget,
          changeOrigin: true,
          secure: false,
          bypass: (req) => {
            const url = req.url || '';
            
            // 1. Always let Vite handle its own internals and source files
            if (url.match(/^\/(@vite|src|node_modules|@react-refresh|@id|@fs)/)) {
              return url;
            }

            // 2. Always proxy API calls to the backend
            if (url.includes('/api/')) {
              return null;
            }

            // 3b. Proxy configuration.json
            if (url.endsWith('/configuration.json')) {
              return null;
            }

            // 3. Let Vite handle the Explorer UI (anything under the base path)
            if (url.startsWith(basePath)) {
              return url;
            }

            // 4. Proxy everything else to the main application (.NET/Webpack)
            return null;
          },
        },
      } : undefined,
      hmr: {
        protocol: 'wss',
        clientPort: 5173,
      },
    },
    build: {
      outDir: '../wwwroot',
      emptyOutDir: true,
    },
  };
});
