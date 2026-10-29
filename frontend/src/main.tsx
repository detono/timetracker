import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import App from "./App";
import { AuthProvider } from "./auth/AuthContext";
import { appConfig } from "./config";
import { darken } from "./utils/color";
import "./styles/global.css";
import './i18n';

// Apply the runtime-configured title and brand colors before the app renders, so a single
// built image can be reused for different deployments/clients just by changing environment
// variables (see docker-entrypoint.sh / .env.example) - no rebuild required.
document.title = appConfig.title;

const root = document.documentElement.style;
root.setProperty("--color-primary", appConfig.colorPrimary);
root.setProperty("--color-on-primary", appConfig.colorOnPrimary);
root.setProperty("--color-primary-dark", darken(appConfig.colorPrimary, 0.18));
root.setProperty("--color-secondary", appConfig.colorSecondary);
root.setProperty("--color-on-secondary", appConfig.colorOnSecondary);
root.setProperty("--color-secondary-dark", darken(appConfig.colorSecondary, 0.12));

// A clock icon in the brand color for the browser tab, and the brand color for the browser bar on phones.
const favicon = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 32"><rect width="32" height="32" rx="8" fill="${appConfig.colorPrimary}"/><circle cx="16" cy="16" r="9" fill="none" stroke="${appConfig.colorOnPrimary}" stroke-width="2.5"/><path d="M16 11v5.5l3.5 2" fill="none" stroke="${appConfig.colorOnPrimary}" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"/></svg>`;
const icon = document.createElement("link");
icon.rel = "icon";
icon.type = "image/svg+xml";
icon.href = `data:image/svg+xml,${encodeURIComponent(favicon)}`;
document.head.appendChild(icon);

const themeColor = document.createElement("meta");
themeColor.name = "theme-color";
themeColor.content = appConfig.colorPrimary;
document.head.appendChild(themeColor);

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <BrowserRouter>
      <AuthProvider>
        <App />
      </AuthProvider>
    </BrowserRouter>
  </React.StrictMode>
);
