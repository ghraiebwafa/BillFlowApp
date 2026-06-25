FROM node:20-alpine AS build
WORKDIR /app

ARG VITE_AUTH_API_URL
ARG VITE_MANAGEMENT_API_URL
RUN test -n "$VITE_AUTH_API_URL" && test -n "$VITE_MANAGEMENT_API_URL"

ENV VITE_AUTH_API_URL=$VITE_AUTH_API_URL
ENV VITE_MANAGEMENT_API_URL=$VITE_MANAGEMENT_API_URL

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:alpine

RUN apk add --no-cache gettext

COPY nginx.conf.template /etc/nginx/templates/default.conf.template
COPY docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 3000
ENTRYPOINT ["/docker-entrypoint.sh"]
