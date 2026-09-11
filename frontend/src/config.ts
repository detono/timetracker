export interface AppConfig {
  title: string;
  colorPrimary: string;
  colorOnPrimary: string;
  colorSecondary: string;
  colorOnSecondary: string;
}

declare global {
  interface Window {
    __APP_CONFIG__?: Partial<AppConfig>;
  }
}

/**
 * Matches the :root defaults in global.css, so there's no visual flash even before this
 * module runs, and so local dev (vite dev server - no docker-entrypoint.sh involved) still
 * looks right without any extra setup.
 */
const defaults: AppConfig = {
  title: "Shiftlog",
  colorPrimary: "#932e4a",
  colorOnPrimary: "#ffffff",
  colorSecondary: "#e4d1dc",
  colorOnSecondary: "#000000"
};

/**
 * In production, config.js is rendered from env vars by docker-entrypoint.sh before nginx
 * ever serves it. In local dev (or if that step is ever skipped), the file still contains
 * literal "${APP_TITLE}"-style placeholders - treat those as "not configured" too, not as
 * real values.
 */
function resolve(value: string | undefined, fallback: string): string {
  if (!value || value.startsWith("${")) {
    return fallback;
  }
  return value;
}

const raw = window.__APP_CONFIG__ ?? {};

export const appConfig: AppConfig = {
  title: resolve(raw.title, defaults.title),
  colorPrimary: resolve(raw.colorPrimary, defaults.colorPrimary),
  colorOnPrimary: resolve(raw.colorOnPrimary, defaults.colorOnPrimary),
  colorSecondary: resolve(raw.colorSecondary, defaults.colorSecondary),
  colorOnSecondary: resolve(raw.colorOnSecondary, defaults.colorOnSecondary)
};
