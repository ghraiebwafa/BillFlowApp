#!/bin/sh
set -eu

: "${VITE_AUTH_API_URL:?VITE_AUTH_API_URL is required}"
: "${VITE_MANAGEMENT_API_URL:?VITE_MANAGEMENT_API_URL is required}"

envsubst '${VITE_AUTH_API_URL} ${VITE_MANAGEMENT_API_URL}' \
  < /etc/nginx/templates/default.conf.template \
  > /etc/nginx/conf.d/default.conf

exec nginx -g 'daemon off;'
