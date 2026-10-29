import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    // `npm run dev` with VITE_API_BASE_URL=/api: the dev server passes API calls on to a local
    // API, so the browser sees one origin and the API's CORS list doesn't need the dev port.
    proxy: {
      "/api": process.env.DEV_API_PROXY ?? "http://localhost:5067"
    }
  },
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: "./src/setupTests.ts"
  }
});
