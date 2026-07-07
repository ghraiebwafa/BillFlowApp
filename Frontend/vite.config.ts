import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

export default defineConfig({
  plugins: [react()],
  build: {
    rollupOptions: {
      output: {
        manualChunks(id) {
          if (!id.includes("node_modules")) return;
          if (id.includes("react-router-dom")) return "router";
          if (id.includes("@tanstack/react-query")) return "query";
          if (id.includes("react-hook-form") || id.includes("@hookform/resolvers")) return "forms";
          if (id.includes("i18next") || id.includes("react-i18next")) return "i18n";
          if (id.includes("zod")) return "validation";
          if (id.includes("lucide-react")) return "icons";
          return "vendor";
        },
      },
    },
  },
  server: {
    host: "0.0.0.0",
    port: 3000,
  },
});
