FROM node:22-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm install

COPY . .
RUN npm run build -- --configuration production

FROM nginx:1.29-alpine AS runtime
WORKDIR /usr/share/nginx/html

COPY docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY docker/99-luminapath-env.sh /docker-entrypoint.d/99-luminapath-env.sh
RUN chmod +x /docker-entrypoint.d/99-luminapath-env.sh

COPY --from=build /app/www/browser ./

ENV LUMINAPATH_API_ENDPOINT=/api
ENV LUMINAPATH_API_PROXY_TARGET=http://luminapath-api:8080

EXPOSE 80
