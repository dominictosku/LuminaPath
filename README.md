# LuminaPath

LuminaPath is a personal game library, backlog planner, playtime tracker and quest-style productivity app. The main app is an ASP.NET Core Blazor experience with a PostgreSQL backend. An optional Ionic/Angular frontend is included for a mobile-style interface and can run against the same API.

## What It Does

- Catalog games with platforms, genres, release dates, covers and estimated playtime.
- Track your personal library with status, priority, rating, start/end dates and manual played hours.
- Keep manually entered playtime separate from third-party playtime such as PSN, while showing a combined total.
- Import/export game data through Excel.
- Import PlayStation Network play history.
- Manage media documents and uploaded cover files.
- Track RPG-style quests, faction quests and real-life skills through the quest board.
- View dashboards and statistics for current games, completed games, played hours and planned releases.

## Stack

- .NET 10 / ASP.NET Core / Blazor Server
- Entity Framework Core and ASP.NET Core Identity
- PostgreSQL
- MudBlazor and Radzen
- Optional Ionic/Angular frontend, built with Node.js 22 LTS
- Docker Compose for local or self-hosted deployment
- GitHub Actions for CI and container publishing

## Quick Docker Setup

Copy the example environment file and adjust passwords/ports if needed:

```powershell
Copy-Item .env.example .env
```

Start the backend and PostgreSQL:

```powershell
docker compose up -d --build
```

Open the Blazor app/API at:

```text
http://localhost:8080
```

Start the optional Angular frontend too:

```powershell
docker compose -f docker-compose.yml -f docker-compose.frontend.yml up -d --build
```

Open Angular at:

```text
http://localhost:4200
```

The Angular container uses `/api` and proxies requests to the backend container, so browser/API communication works without local CORS pain.

Optional pgAdmin:

```powershell
docker compose -f docker-compose.yml -f docker-compose.tools.yml up -d
```

Open pgAdmin at `http://localhost:5050`.

## Environment Variables

All deployment-specific values are controlled through `.env` or normal ASP.NET environment variables.

Core Docker variables:

```text
BACKEND_HTTP_PORT=8080
FRONTEND_HTTP_PORT=4200
POSTGRES_PORT=5432
POSTGRES_DB=luminapath
POSTGRES_USER=luminapath
POSTGRES_PASSWORD=change-me
```

Backend variables:

```text
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__Default=Host=db;Port=5432;Database=luminapath;Username=luminapath;Password=change-me;
RUN_MIGRATIONS_ON_STARTUP=true
HTTPS_REDIRECT=false
Storage__Provider=FileSystem
Storage__Path=/app/App_Data/storage
Cors__AllowedOrigins__0=http://localhost:4200
```

Azure Blob storage, if you do not want file-system storage:

```text
Storage__Provider=Azure
Azure__BlobConnectionString=...
Azure__BlobContainerName=media
```

Angular runtime variable:

```text
LUMINAPATH_API_ENDPOINT=/api
```

For the Docker frontend, `/api` is recommended because nginx proxies it to the backend service. Outside Docker, set it to something like `https://your-api.example.com/api`.

## Local Development Without Docker

Start PostgreSQL through Docker:

```powershell
docker compose up -d db
```

Run the Blazor backend:

```powershell
dotnet restore LuminaPath.sln
dotnet run --project src/LuminaPath/LuminaPath.csproj
```

Run the Angular frontend:

```powershell
cd src/LuminaPath.MobileApp
npm install
npm start
```

For local Angular development, edit `src/LuminaPath.MobileApp/src/assets/env.js` if you need a different API endpoint.

## CI/CD

GitHub Actions are split into two workflows:

- `CI`: restores, builds and tests the .NET solution, builds Angular, validates Compose and builds both Docker images.
- `Publish Containers`: publishes backend and frontend images to GitHub Container Registry on version tags like `v1.2.3` or manual dispatch.

Optional Azure App Service deployment is supported by setting these repository secrets:

```text
AZURE_CREDENTIALS
AZURE_WEBAPP_NAME
```

When `AZURE_WEBAPP_NAME` is present, the publish workflow deploys the API image to that Azure Web App.

## Seeded Login

Development seeding creates:

```text
Email:    admin@example.com
Password: Admin123*
```

Roles:

```text
Administrator
Editor
```

## Tests

```powershell
dotnet test Test/Test.csproj
```

On Windows, if another process locks normal build output, use:

```powershell
dotnet test Test/Test.csproj -p:OutDir=.\artifacts\test-out\
```
