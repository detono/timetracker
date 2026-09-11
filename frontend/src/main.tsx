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

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <BrowserRouter>
      <AuthProvider>
        <App />
      </AuthProvider>
    </BrowserRouter>
  </React.StrictMode>
);
