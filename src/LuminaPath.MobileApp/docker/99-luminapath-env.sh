#!/bin/sh
set -eu

: "${LUMINAPATH_API_ENDPOINT:=/api}"
: "${LUMINAPATH_API_PROXY_TARGET:=http://luminapath-api:8080}"

cat >/usr/share/nginx/html/assets/env.js <<EOF
window.__LUMINAPATH_CONFIG__ = {
  apiEndpoint: "${LUMINAPATH_API_ENDPOINT}"
};
EOF

envsubst '${LUMINAPATH_API_PROXY_TARGET}' </etc/nginx/conf.d/default.conf >/tmp/luminapath-nginx.conf
mv /tmp/luminapath-nginx.conf /etc/nginx/conf.d/default.conf
