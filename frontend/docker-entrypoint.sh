#!/bin/sh
set -e

# Render the runtime branding config from environment variables (title + colors) into a
# real config.js the app fetches on load. This runs every container start, not at image
# build time, which is what makes the same image reusable across deployments - change the
# env vars, restart the container, no rebuild required.
envsubst '${APP_TITLE} ${COLOR_PRIMARY} ${COLOR_ON_PRIMARY} ${COLOR_SECONDARY} ${COLOR_ON_SECONDARY}' \
  < /usr/share/nginx/html/config.template.js \
  > /usr/share/nginx/html/config.js

exec "$@"
