<div align="center">
  <img src="docs/images/app-screenshot-placeholder.svg" alt="LuminaPath app screenshot placeholder" width="100%" />

  <h1>LuminaPath</h1>

  <p>
    <strong>Your games, movies, series and anime library, backlog, playtime tracker, social hub and quest board in one self-hosted command center.</strong>
  </p>

  <p>
    <a href="https://github.com/dominictosku/LuminaPath/stargazers">
      <img alt="GitHub stars" src="https://img.shields.io/github/stars/dominictosku/LuminaPath?style=for-the-badge&logo=github&color=38bdf8">
    </a>
    <a href="https://github.com/dominictosku/LuminaPath/network/members">
      <img alt="GitHub forks" src="https://img.shields.io/github/forks/dominictosku/LuminaPath?style=for-the-badge&logo=github&color=60a5fa">
    </a>
    <a href="https://github.com/dominictosku/LuminaPath/issues">
      <img alt="GitHub issues" src="https://img.shields.io/github/issues/dominictosku/LuminaPath?style=for-the-badge&logo=github&color=f59e0b">
    </a>
    <a href="https://github.com/dominictosku/LuminaPath/actions/workflows/ci.yml">
      <img alt="CI" src="https://img.shields.io/github/actions/workflow/status/dominictosku/LuminaPath/ci.yml?branch=main&style=for-the-badge&logo=githubactions&label=CI">
    </a>
    <a href="https://github.com/dominictosku/LuminaPath/actions/workflows/publish.yml">
      <img alt="Release" src="https://img.shields.io/github/actions/workflow/status/dominictosku/LuminaPath/publish.yml?style=for-the-badge&logo=githubactions&label=Release">
    </a>
  </p>

  <p>
    <img alt=".NET 10" src="https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet">
    <img alt="Blazor" src="https://img.shields.io/badge/Blazor-Server-512BD4?style=flat-square&logo=blazor">
    <img alt="Angular" src="https://img.shields.io/badge/Angular-21-DD0031?style=flat-square&logo=angular">
    <img alt="PostgreSQL" src="https://img.shields.io/badge/PostgreSQL-17-4169E1?style=flat-square&logo=postgresql&logoColor=white">
    <img alt="Docker" src="https://img.shields.io/badge/Docker-ready-2496ED?style=flat-square&logo=docker&logoColor=white">
  </p>

  <p>
    <a href="#quick-docker-setup">Quick Start</a>
    <span> | </span>
    <a href="#features">Features</a>
    <span> | </span>
    <a href="#environment-variables">Configuration</a>
    <span> | </span>
    <a href="#ai-assistant">AI Assistant</a>
    <span> | </span>
    <a href="#cicd">CI/CD</a>
  </p>
</div>

> Screenshot note: replace `docs/images/app-screenshot-placeholder.svg` with your real app screenshot, or add `docs/images/app-screenshot.png` and update the image path above.

## Overview

LuminaPath is a personal media library, backlog planner, playtime tracker, social hub and quest-style productivity app. It tracks games, movies, series and anime in one place, layers a friends/DM/co-op-session social layer on top, and adds an RPG-style quest and skill-tree system for real-life goals. The main experience is an ASP.NET Core Blazor app with a PostgreSQL backend. An optional Ionic/Angular frontend is included for a mobile-style interface and can run against the same API.

<table>
  <tr>
    <td><strong>Library</strong><br />Catalog games, movies, series and anime with covers, platforms, genres and release dates.</td>
    <td><strong>Playtime</strong><br />Track manual hours next to third-party playtime such as PSN and Steam.</td>
    <td><strong>Quest Board</strong><br />Plan main quests, side quests, factions and real-life skill trees.</td>
  </tr>
  <tr>
    <td><strong>Dashboards</strong><br />See what you are playing/watching, what is finished and what is ahead.</td>
    <td><strong>Social</strong><br />Add friends, send direct messages and schedule co-op gaming sessions.</td>
    <td><strong>Imports</strong><br />Bring in data from Excel, PlayStation Network and Steam.</td>
  </tr>
  <tr>
    <td><strong>AI Assistant</strong><br />Streaming chat with read-only DB tools and MCP server support.</td>
    <td><strong>Release Calendar</strong><br />Browse upcoming releases and plan ahead.</td>
    <td><strong>Self-hosting</strong><br />Run the backend, database and optional Angular app through Docker.</td>
  </tr>
</table>

## Features

- Multi-media catalog covering games, movies, series and anime, with platforms, genres, release dates, cover images and estimated playtime/runtime.
- Personal tracking per media type with status, priority, rating, start/end dates and manual played/watched hours.
- Manual playtime and third-party playtime stay separate, while the UI shows a combined total.
- Excel import/export for library and play history data.
- PlayStation Network and Steam import flows.
- Browse and release-calendar views for discovering and planning upcoming titles.
- Social layer: friends, direct messages and scheduled co-op gaming sessions.
- Media document management for uploaded covers and files.
- RPG-style quest board with main quests, sub quests, faction quests and skill trees for real-life goals.
- Dashboard and statistics pages for played hours, completions and upcoming releases.
- ASP.NET Core Identity authentication with seeded administrator/editor roles.
- File-system storage by default, with Azure Blob support available through env vars.
- Streaming AI assistant in the Angular app, powered by Anthropic Claude or any OpenAI-compatible endpoint (Ollama, LM Studio, OpenAI), with MCP tool integration.

## Documentation

Full project documentation lives in [`docs/`](docs/index.md) — architecture (C4), decisions (ADRs), feature deep-dives, and runbooks. Preview locally with `mkdocs serve` (see [`mkdocs.yml`](mkdocs.yml)).

## Tech Stack

| Layer | Tools |
| --- | --- |
| Backend | .NET 10, ASP.NET Core, Blazor Server, EF Core, Identity |
| Frontend | MudBlazor, Radzen, optional Ionic/Angular 21 |
| Data | PostgreSQL 17 |
| Deployment | Docker, Docker Compose, GitHub Actions, optional Azure Web App |
| Testing | xUnit |

## Quick Docker Setup

Use the lightweight deployment files if you want to run LuminaPath from prebuilt Docker Hub images:

```powershell
cd deploy
Copy-Item .env.example .env
```

Edit `.env` and set `POSTGRES_PASSWORD`, `LUMINAPATH_ADMIN_EMAIL`,
and `LUMINAPATH_ADMIN_PASSWORD`, then start:

```powershell
docker compose --env-file .env up -d
```

Open Angular at `http://localhost:4200`. The deploy frontend proxies `/api`,
`/Account`, and `/Admin` to the backend internally. For direct local backend
access, add the optional backend-port overlay:

```powershell
docker compose --env-file .env -f docker-compose.yml -f docker-compose.backend.yml up -d
```

Use the root Compose files below when you want to build the images yourself from source.

Create `.env` with the same required production values:

```text
POSTGRES_PASSWORD=your-strong-password
LUMINAPATH_ADMIN_EMAIL=you@example.com
LUMINAPATH_ADMIN_PASSWORD=your-strong-admin-password
```

`FRONTEND_PUBLIC_URL` is optional when the included frontend proxies `/api`
same-origin. Set it only when a browser talks to the backend API from a
different public origin; cross-site cookie deployments should also set
`AUTH_COOKIE_SAMESITE=None` with `AUTH_COOKIE_SECURE_POLICY=Always`.

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

Optional local vLLM:

```powershell
docker compose --profile amd -f docker-compose.yml -f docker-compose.vllm.yml up -d --build
```

Use `--profile nvidia` instead on NVIDIA hosts. This starts `vllm` at `http://localhost:8000/v1` and points the backend AI assistant at it through the OpenAI-compatible provider. The default model is `NousResearch/Hermes-3-Llama-3.1-8B` with the `hermes` tool-call parser, so tool calling works out of the box when the selected model/parser pair supports it. Change `VLLM_MODEL`, `VLLM_TOOL_CALL_PARSER`, `VLLM_GPU_MEMORY_UTILIZATION` and `VLLM_TENSOR_PARALLEL_SIZE` in `.env` for your GPU and model.

## Environment Variables

All deployment-specific values are controlled through `.env` or normal ASP.NET environment variables.
Most knobs have defaults in Docker Compose or the backend option classes.
The full optional reference lives in [`docs/05-runbooks/configuration.md`](docs/05-runbooks/configuration.md).

Required Docker variables:

```text
POSTGRES_PASSWORD=your-strong-password
LUMINAPATH_ADMIN_EMAIL=you@example.com
LUMINAPATH_ADMIN_PASSWORD=your-strong-admin-password
```

Common optional Docker variables:

```text
BACKEND_HTTP_PORT=8080
FRONTEND_HTTP_PORT=4200
RUN_MIGRATIONS_ON_STARTUP=false
AUTH_REQUIRE_ADMIN_APPROVAL=true
FRONTEND_PUBLIC_URL=https://luminapath.yourdomain.com
```

Azure Blob storage, if you do not want file-system storage:

```text
STORAGE_PROVIDER=Azure
AZURE_BLOB_CONNECTION_STRING=...
AZURE_BLOB_CONTAINER_NAME=media
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

## Manual Android APK Build

Use this when you want to build the Ionic/Angular mobile app yourself and install it on a phone for homelab testing.

Install these tools first:

- Android Studio with the Android SDK.
- Java 21.
- Android platform tools if you want to install through `adb`.
- Node.js 22.

Build the web app and sync Capacitor:

```powershell
cd src/LuminaPath.MobileApp
npm install
npm run build -- --configuration production
npx cap add android
npx cap sync android
```

`npx cap add android` only needs to be run once, or whenever the generated `android` folder does not exist.
The Angular production build writes the web app to `www/browser`, which is the directory Capacitor packages into the APK.

Build a debug APK:

```powershell
cd android
.\gradlew.bat assembleDebug
```

The APK is created here:

```text
src/LuminaPath.MobileApp/android/app/build/outputs/apk/debug/app-debug.apk
```

Install it on a connected Android device:

```powershell
adb install -r app\build\outputs\apk\debug\app-debug.apk
```

For homelab testing, point the app login screen to your hosted backend API:

```text
https://luminapath.yourdomain.com/api
```

or:

```text
http://192.168.178.50:8080/api
```

HTTPS with a trusted certificate is recommended. Plain HTTP and self-signed certificates can work for some setups, but Android WebView is much stricter than a normal desktop browser.

Allow the Capacitor app origin in the backend CORS configuration:

```text
Cors__AllowedOrigins__0=capacitor://localhost
Cors__AllowedOrigins__1=http://localhost
Cors__AllowedOrigins__2=https://luminapath.yourdomain.com
Auth__CookieSameSite=None
Auth__CookieSecurePolicy=Always
```

Restart the backend after changing CORS settings.

For quick rebuilds after frontend changes:

```powershell
cd src/LuminaPath.MobileApp
npm run build -- --configuration production
npx cap sync android
cd android
.\gradlew.bat assembleDebug
adb install -r app\build\outputs\apk\debug\app-debug.apk
```

## AI Assistant

The Angular app ships with a floating chat panel (bottom-right) that streams replies token-by-token from a backend endpoint at `POST /api/chat/stream`. The agent has access to read-only database tools scoped to the signed-in user, plus optional MCP tools for external information.

### Pick a provider

The provider is selected through `AiChat:Provider`. Both providers are wired by default — switch by changing config and supplying the credentials.

| Provider | `AiChat:Provider` | Used for |
| --- | --- | --- |
| Anthropic Claude | `anthropic` (default) | Claude 4 family via the official API |
| OpenAI-compatible | `openai` (or `ollama`) | Ollama, LM Studio, vLLM, llama.cpp server, the official OpenAI API, etc. |

#### Anthropic Claude

```text
ANTHROPIC_API_KEY=sk-ant-...
```

Or in `appsettings.Development.json`:

```jsonc
"AiChat":   { "Provider": "anthropic" },
"Anthropic": {
  "ApiKey": "sk-ant-...",
  "Model": "claude-sonnet-4-6",
  "MaxTokens": 4096
}
```

#### Ollama (self-hosted)

Install Ollama and pull a tool-capable model (e.g. `llama3.2`, `qwen2.5`, `mistral-small`):

```powershell
ollama pull llama3.2
ollama serve
```

Then point the backend at it:

```jsonc
"AiChat": { "Provider": "ollama" },
"OpenAi": {
  "BaseUrl": "http://localhost:11434/v1",
  "Model":   "llama3.2"
}
```

No API key required. Ollama exposes the OpenAI-compatible Chat Completions API at `/v1/chat/completions`, including tool calls.

#### Other OpenAI-compatible endpoints

```jsonc
"AiChat": { "Provider": "openai" },
"OpenAi": {
  "ApiKey":  "sk-...",                // empty for keyless local servers
  "BaseUrl": "https://api.openai.com/v1",
  "Model":   "gpt-4o-mini",
  "ToolChoice": "auto"                // optional; useful for vLLM auto tool calling
}
```

For LM Studio, vLLM or llama.cpp's server, change `BaseUrl` to whatever they expose (typically ending in `/v1`) and pick the model name they advertise.

For local GPU testing with vLLM:

```powershell
docker compose --profile amd -f docker-compose.yml -f docker-compose.vllm.yml up -d --build
```

or:

```powershell
docker compose --profile nvidia -f docker-compose.yml -f docker-compose.vllm.yml up -d --build
```

The overlay has two mutually exclusive vLLM profiles:

| Profile | Image default | GPU wiring |
| --- | --- | --- |
| `amd` | `vllm/vllm-openai-rocm:latest` | Exposes `/dev/kfd` and `/dev/dri`; uses `HIP_VISIBLE_DEVICES` / `ROCR_VISIBLE_DEVICES` |
| `nvidia` | `vllm/vllm-openai:latest` | Uses Docker Compose `gpus`; sets `NVIDIA_VISIBLE_DEVICES` / `NVIDIA_DRIVER_CAPABILITIES` |

Both profiles publish `http://localhost:8000/v1` on the host and expose `http://vllm:8000/v1` inside Docker, so the backend config stays the same. Set `COMPOSE_PROFILES=amd` or `COMPOSE_PROFILES=nvidia` in `.env` if you do not want to pass `--profile` every time. The default tool-calling setup is Hermes-flavored; if you switch to a Llama, Mistral, Qwen or other model, update `VLLM_TOOL_CALL_PARSER` to the parser that matches that model.

### Environment variable overrides

These take precedence over `appsettings.json` so you can leave the file blank in source control:

```text
ANTHROPIC_API_KEY=...     # falls into Anthropic:ApiKey
OPENAI_API_KEY=...        # falls into OpenAi:ApiKey
OPENAI_BASE_URL=...       # falls into OpenAi:BaseUrl
OPENAI_MODEL=...          # falls into OpenAi:Model
```

### Built-in database tools

The agent can call these tools (all read-only and scoped to the authenticated user):

| Tool | Purpose |
| --- | --- |
| `list_upcoming_releases` | Future game releases, optional month/year filter, optional `only_my_library` |
| `search_my_library` | Search games in the user's library by name fragment, status or platform |
| `library_summary` | Counts by status, total logged hours, estimated backlog hours |
| `my_quests` | Quests for the user, filterable by `open` / `completed` / `all` |
| `my_gaming_sessions` | Scheduled sessions in a date window (default: next 14 days) |

Tool use works with both Anthropic and any OpenAI-compatible model that supports the `tools` / `tool_calls` API.

### MCP servers

External tools can be added through Model Context Protocol servers. Configure them under `Mcp:Servers`. Each server is launched as a subprocess via stdio, so the host machine needs the relevant runtime (e.g. `npx` for Node-based servers).

```jsonc
"Mcp": {
  "Servers": [
    {
      "Name": "brave-search",
      "Enabled": true,
      "Command": "npx",
      "Args": [ "-y", "@modelcontextprotocol/server-brave-search" ],
      "Env": { "BRAVE_API_KEY": "" }
    }
  ]
}
```

Servers with empty env values are skipped at startup, so the chat keeps working without them. To enable Brave Search:

```text
BRAVE_API_KEY=...
```

MCP tools appear to the model alongside the built-in ones, namespaced as `mcp__<server>__<tool>` to avoid name clashes. Add more servers (filesystem, time, fetch, custom MCP servers) by appending entries to the `Servers` array.

### Tool iteration limit

The agent will execute up to `AiChat:MaxToolIterations` (default `8`) tool round-trips before forcing a final answer. Bump this if you expect long multi-step reasoning chains.

## CI/CD

GitHub Actions are split into two workflows:

- `CI`: restores, builds and tests the .NET solution, builds Angular, validates Compose and builds both Docker images.
- `Release`: publishes backend and frontend images to Docker Hub, builds the Ionic/Angular app as an Android APK, and attaches the APK to a GitHub Release.

Create a release by pushing a version tag:

```powershell
git tag v1.2.3
git push origin v1.2.3
```

Docker Hub publishing needs these repository secrets:

```text
DOCKERHUB_USERNAME
DOCKERHUB_TOKEN
```

The release workflow publishes:

```text
docker.io/<DOCKERHUB_USERNAME>/luminapath-api:<version>
docker.io/<DOCKERHUB_USERNAME>/luminapath-api:<major>.<minor>
docker.io/<DOCKERHUB_USERNAME>/luminapath-api:latest
docker.io/<DOCKERHUB_USERNAME>/luminapath-frontend:<version>
docker.io/<DOCKERHUB_USERNAME>/luminapath-frontend:<major>.<minor>
docker.io/<DOCKERHUB_USERNAME>/luminapath-frontend:latest
```

Every build also gets a `sha-...` tag for exact rollbacks.

Android APK publishing works without signing secrets and uploads a debug-signed APK. For a user-facing release APK, add these optional secrets:

```text
ANDROID_KEYSTORE_BASE64
ANDROID_KEYSTORE_PASSWORD
ANDROID_KEY_ALIAS
ANDROID_KEY_PASSWORD
```

`ANDROID_KEYSTORE_BASE64` is your Android keystore encoded as base64. When all four secrets are present, the workflow signs, verifies and uploads `LuminaPath-<version>.apk` to the GitHub Release.

## Seeded Login

Development fallback seeding creates:

```text
Email:    admin@example.com
Password: ChangeMe!1AdminAccess
```

Production startup refuses those bundled credentials, so set
`LUMINAPATH_ADMIN_EMAIL` and `LUMINAPATH_ADMIN_PASSWORD` before first boot.

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

## Migrations

Development applies migrations automatically when the backend runs with `ASPNETCORE_ENVIRONMENT=Development`.
Production should keep `RUN_MIGRATIONS_ON_STARTUP=false`, generate an idempotent SQL script, review it, and apply it manually.

Create a migration:

```powershell
dotnet ef migrations add BackgroundJobs --project src/LuminaPath.Infrastructure/LuminaPath.Infrastructure.csproj --startup-project src/LuminaPath/LuminaPath.csproj --output-dir Migrations
```

See the full runbook: [`docs/05-runbooks/migrations.md`](docs/05-runbooks/migrations.md).

## Upgrades

For major dependency upgrades such as .NET 10 to 11 or Angular 21 to 22, use the upgrade checklist:

[`docs/05-runbooks/updating-the-app.md`](docs/05-runbooks/updating-the-app.md)
