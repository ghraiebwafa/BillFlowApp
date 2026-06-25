FROM node:20-alpine AS build
WORKDIR /app

ARG VITE_AUTH_API_URL=http://localhost:5237
ARG VITE_MANAGEMENT_API_URL=http://localhost:5177
ENV VITE_AUTH_API_URL=$VITE_AUTH_API_URL
ENV VITE_MANAGEMENT_API_URL=$VITE_MANAGEMENT_API_URL

COPY package*.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 3000
CMD ["nginx", "-g", "daemon off;"]
