FROM node:22-alpine AS build
WORKDIR /src/src/LuminaPath.MobileApp

COPY src/LuminaPath.MobileApp/package*.json ./
RUN npm ci

COPY LICENSE THIRD_PARTY_NOTICES.md /src/
COPY legal/ /src/legal/
COPY licenses/ /src/licenses/
COPY scripts/ /src/scripts/
COPY src/LuminaPath.MobileApp/ ./
ARG SOURCE_REVISION=
ARG SOURCE_REPOSITORY=https://github.com/dominictosku/LuminaPath
ARG SOURCE_URL=
RUN npm run build -- --configuration production

FROM nginx:1.29-alpine AS runtime
ARG SOURCE_REVISION=
ARG SOURCE_REPOSITORY=https://github.com/dominictosku/LuminaPath
LABEL org.opencontainers.image.licenses="AGPL-3.0-only" \
      org.opencontainers.image.source=$SOURCE_REPOSITORY \
      org.opencontainers.image.revision=$SOURCE_REVISION
WORKDIR /usr/share/nginx/html

COPY src/LuminaPath.MobileApp/docker/nginx.conf /etc/nginx/conf.d/default.conf
COPY src/LuminaPath.MobileApp/docker/99-luminapath-env.sh /docker-entrypoint.d/99-luminapath-env.sh
RUN chmod +x /docker-entrypoint.d/99-luminapath-env.sh

COPY --from=build /src/src/LuminaPath.MobileApp/www/browser ./

ENV LUMINAPATH_API_ENDPOINT=/api
ENV LUMINAPATH_API_PROXY_TARGET=http://luminapath-api:8080

EXPOSE 80
