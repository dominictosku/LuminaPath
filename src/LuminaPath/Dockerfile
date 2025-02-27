#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
WORKDIR /app
EXPOSE 8081
EXPOSE 8082

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/LuminaPath/LuminaPath.csproj", "src/LuminaPath/"]
COPY ["src/LuminaPath.Core/LuminaPath.Core.csproj", "src/LuminaPath.Core/"]
COPY ["src/LuminaPath.Infrastructure/LuminaPath.Infrastructure.csproj", "src/LuminaPath.Infrastructure/"]
COPY ["src/LuminaPath.UI.Shared/LuminaPath.UI.Shared.csproj", "src/LuminaPath.UI.Shared/"]
RUN dotnet restore "./src/LuminaPath/LuminaPath.csproj"
COPY . .
WORKDIR "/src/src/LuminaPath"
RUN dotnet build "./LuminaPath.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./LuminaPath.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LuminaPath.dll"]