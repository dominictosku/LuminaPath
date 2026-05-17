FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY LuminaPath.sln ./
COPY src/LuminaPath/LuminaPath.csproj src/LuminaPath/
COPY src/LuminaPath.Core/LuminaPath.Core.csproj src/LuminaPath.Core/
COPY src/LuminaPath.Infrastructure/LuminaPath.Infrastructure.csproj src/LuminaPath.Infrastructure/
RUN dotnet restore src/LuminaPath/LuminaPath.csproj

COPY . .
WORKDIR /src/src/LuminaPath
RUN dotnet publish LuminaPath.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl postgresql-client \
    && rm -rf /var/lib/apt/lists/*
RUN mkdir -p /app/App_Data/storage
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "LuminaPath.dll"]
