FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY LuminaPath.sln ./
COPY Directory.Build.props Directory.Packages.props ./
COPY src/LuminaPath/LuminaPath.csproj src/LuminaPath/
COPY src/LuminaPath.Core/LuminaPath.Core.csproj src/LuminaPath.Core/
COPY src/LuminaPath.Infrastructure/LuminaPath.Infrastructure.csproj src/LuminaPath.Infrastructure/
RUN dotnet restore src/LuminaPath/LuminaPath.csproj

COPY . .
WORKDIR /src/src/LuminaPath
RUN dotnet publish LuminaPath.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM node:22-alpine AS legal
WORKDIR /src
COPY LICENSE THIRD_PARTY_NOTICES.md ./
COPY legal/ legal/
COPY licenses/ licenses/
COPY scripts/prepare-legal.mjs scripts/prepare-legal.mjs
COPY scripts/check-backend-licenses.mjs scripts/check-backend-licenses.mjs
COPY --from=build /app/publish/LuminaPath.deps.json /deps.json
ARG SOURCE_REVISION=
ARG SOURCE_REPOSITORY=https://github.com/dominictosku/LuminaPath
ARG SOURCE_URL=
RUN node scripts/check-backend-licenses.mjs /deps.json && node scripts/prepare-legal.mjs /legal

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG SOURCE_REVISION=
ARG SOURCE_REPOSITORY=https://github.com/dominictosku/LuminaPath
LABEL org.opencontainers.image.licenses="AGPL-3.0-only" \
      org.opencontainers.image.source=$SOURCE_REPOSITORY \
      org.opencontainers.image.revision=$SOURCE_REVISION
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl postgresql-client \
    && rm -rf /var/lib/apt/lists/*
RUN mkdir -p /app/App_Data/storage /app/App_Data/logs
COPY --from=build /app/publish .
COPY --from=legal /legal/ /app/wwwroot/legal/

ENTRYPOINT ["dotnet", "LuminaPath.dll"]
