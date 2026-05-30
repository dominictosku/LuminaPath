#!/bin/sh
set -eu

: "${LUMINAPATH_API_ENDPOINT:=/api}"
: "${LUMINAPATH_API_PROXY_TARGET:=http://luminapath-api:8080}"

# Enforcing Content-Security-Policy for the served SPA document. Scripts are
# locked to same-origin (the Angular build emits no inline <script>); styles
# need 'unsafe-inline' because Angular/Ionic inject <style> tags at runtime;
# img-src allows https:/data: for external cover art. connect-src is 'self'
# because the API is proxied same-origin by default — override the whole
# policy via LUMINAPATH_CSP when the SPA talks to a different API origin.
: "${LUMINAPATH_CSP:=default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'none'; form-action 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' data:; connect-src 'self'; worker-src 'self' blob:; manifest-src 'self'}"
export LUMINAPATH_CSP

cat >/usr/share/nginx/html/assets/env.js <<EOF
window.__LUMINAPATH_CONFIG__ = {
  apiEndpoint: "${LUMINAPATH_API_ENDPOINT}"
};
EOF

envsubst '${LUMINAPATH_API_PROXY_TARGET} ${LUMINAPATH_CSP}' </etc/nginx/conf.d/default.conf >/tmp/luminapath-nginx.conf
mv /tmp/luminapath-nginx.conf /etc/nginx/conf.d/default.conf
