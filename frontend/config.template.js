// Filled in from environment variables by docker-entrypoint.sh at container start, not
// baked in at build time - so the same built image can be reused for different clients
// just by changing environment variables, no rebuild needed. See .env.example.
window.__APP_CONFIG__ = {
  title: "${APP_TITLE}",
  colorPrimary: "${COLOR_PRIMARY}",
  colorOnPrimary: "${COLOR_ON_PRIMARY}",
  colorSecondary: "${COLOR_SECONDARY}",
  colorOnSecondary: "${COLOR_ON_SECONDARY}"
};
