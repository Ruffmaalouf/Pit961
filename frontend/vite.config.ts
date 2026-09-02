import path from 'node:path';
import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, '.'),
    },
  },
  server: {
    // CORS on the backend is configured for this exact origin.
    port: 5173,
    strictPort: true,
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./vitest.setup.ts'],
    css: false,
    include: ['**/*.test.{ts,tsx}'],
    exclude: ['node_modules/**', 'e2e/**', 'dist/**'],
    // Pinned to a single forked worker: the default `threads` pool hangs
    // indefinitely partway through the suite on constrained (2-core) CI/dev
    // runners, even though every test that does run passes. This is a worker
    // resource-contention issue in that environment, not a test defect.
    // Verified on the real device before this WP-8 acceptance was signed off.
    //
    // Vitest 4 removed `poolOptions.forks.singleFork` — the equivalent is
    // `fileParallelism: false`, which forces `maxWorkers` to 1 regardless of
    // pool. See https://vitest.dev config migration notes for v4.
    pool: 'forks',
    fileParallelism: false,
  },
});
