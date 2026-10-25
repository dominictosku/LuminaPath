<div align="center">
  <img src="docs/images/Luminapath_Logo.png" alt="LuminaPath logo" width="180" />

  <h1>LuminaPath</h1>

  <p>
    <img alt="Not production ready" src="https://img.shields.io/badge/Not_production_ready-red?style=for-the-badge">
    <br />
    <strong>This is my personal learning project and is not ready for production use.</strong>
  </p>

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
    <span> | </span>
    <a href="#license">License</a>
  </p>
</div>


## Overview

LuminaPath is a personal media library, backlog planner, playtime tracker, social hub and quest-style productivity app. It tracks games, movies, series and anime in one place, layers a friends/DM/co-op-session social layer on top, and adds an RPG-style quest and skill-tree system for real-life goals. The everyday interface is an Ionic/Angular app backed by ASP.NET Core and PostgreSQL; Blazor provides the admin and account pages. Both interfaces are included in the default Docker stack.

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
    <td><strong>AI Assistant</strong><br />Streaming chat with user-scoped DB tools (reads + confirm-first writes) and MCP server support.</td>
    <td><strong>Release Calendar</strong><br />Browse upcoming releases and plan ahead.</td>
    <td><strong>Self-hosting</strong><br />Run the backend, database and Angular app through Docker.</td>
  </tr>
</table>

## Features

- Multi-media catalog covering games, movies, series and anime, with platforms, genres, release dates, cover images and estimated playtime/runtime.
- Personal tracking per media type with status, priority, rating, start/end dates and manual played/watched hours.
- Manual playtime and third-party playtime stay separate, while the UI shows a combined total.
- Excel import/export for library and play history data.
- PlayStation Network and Steam import flows.
- Optional Google Calendar integration: link a Google account from Settings and push your library's upcoming releases and dated quests into a dedicated calendar.
- Browse and release-calendar views for discovering and planning upcoming titles.
- Social layer: friends, direct messages and scheduled co-op gaming sessions.
- Media document management for uploaded covers and files.
- RPG-style quest board with main quests, sub quests, faction quests and skill trees for real-life goals.
- Dashboard and statistics pages for played hours, completions and upcoming releases.
- ASP.NET Core Identity authentication with seeded administrator/editor roles.
- File-system storage by default, with Azure Blob and private Google Drive providers available through env vars.
- Streaming AI assistant in the Angular app, powered by Anthropic Claude or any OpenAI-compatible endpoint (Ollama, LM Studio, OpenAI), with MCP tool integration.

## Documentation

Full project documentation lives in [`docs/`](docs/index.md) — architecture (C4), decisions (ADRs), feature deep-dives, and runbooks. Preview locally with `mkdocs serve` (see [`mkdocs.yml`](mkdocs.yml)).

## Tech Stack

| Layer | Tools |
| --- | --- |
| Backend | .NET 10, ASP.NET Core, Blazor Server, EF Core, Identity |
| Frontend | MudBlazor, Radzen, Ionic/Angular 21 |
| Data | PostgreSQL 17 |
| Deployment | Docker, Docker Compose, GitHub Actions, optional Azure Web App |
| Testing | xUnit |

## Quick Docker Setup

Install Docker with Compose v2.30+ and run from the repository root. For a
standalone install, only `compose.yml` and `.env.example` are needed; optional
overlays and the proxy configuration are needed only when using those features.

```sh
cp .env.example .env
```

On PowerShell use `Copy-Item .env.example .env`. Fill in the three values:

```dotenv
POSTGRES_PASSWORD=your-strong-database-password
LUMINAPATH_ADMIN_EMAIL=you@example.com
LUMINAPATH_ADMIN_PASSWORD=your-strong-admin-password
```

The admin password needs at least 10 characters, four distinct characters,
uppercase, lowercase, a digit and a symbol. Then start the complete app:

```sh
docker compose up -d
```

Open <http://localhost:4200>. The frontend proxies API, account and admin routes
to the backend. The API, PostgreSQL and Redis have no host ports by default.
Use HTTPS through your reverse proxy for access from other machines; secure
cookies remain enabled. No public URL or CORS setting is needed for the default
same-origin deployment.

Startup migrations are enabled in Docker so a fresh database initializes
automatically. Set `Database__RunMigrationsOnStartup=false` if you apply reviewed
SQL migrations yourself; see the [migration runbook](docs/05-runbooks/migrations.md).
New registrations require admin approval by default. Changing bootstrap values
does not rotate an existing account's password; use account management for that.

### Updating

Back up PostgreSQL and uploaded files before upgrading. To pin both application
images to a matching published version, add `LUMINAPATH_VERSION=1.2.3` to `.env`
(replace the example with your release). Without this, the tag is `latest`.

```sh
docker compose pull
docker compose up -d
```

Use the same overlays/profiles on updates as on installation. If schema changes
have been applied, reverting an image may also require restoring its database
backup. Dependency upgrades are covered in the
[upgrade runbook](docs/05-runbooks/updating-the-app.md).

### Optional services

- `docker compose --profile tools up -d` adds pgAdmin at
  <http://localhost:5050>, bound to loopback. Default login:
  `admin@example.com` / `admin`; connect it to host `db` using your database credentials.
- `docker compose --profile proxy up -d` adds the existing TLS nginx example.
  Supply `nginx/nginx.conf` and certificates at `nginx/certs/cert.pem` and
  `nginx/certs/key.pem`. Adapt its `angular.localhost` / `blazor.localhost`
  server names to your domains. An existing external reverse proxy can instead
  forward directly to the frontend port.
- `docker compose -f compose.yml -f compose.vllm.yml --profile amd up -d`
  adds local vLLM. Use `nvidia` instead of `amd` for NVIDIA hardware, and enable
  only one GPU profile. The overlay also points the API at vLLM, so always
  select a GPU profile when using it. GPU drivers and sufficient model memory
  are required on the host.

### File-based secrets

Create `secrets/postgres_password.txt` and `secrets/admin_password.txt` with the
passwords. Keep `LUMINAPATH_ADMIN_EMAIL` in `.env`, but leave its two password
values blank and remove any direct `Database__Password` / `Admin__Password`
overrides. Then run:

```sh
docker compose -f compose.yml -f compose.secrets.yml up -d
```

Secret files are mounted only into the services that need them and are excluded
from Git and Docker build contexts. For a standalone deployment, copy the secrets
overlay too. Host paths can be overridden using the variables in the reference below.

### Migrating an existing installation

Before updating the checkout, stop the old stack using its original files and
`.env` location (`deploy/` for published-image installations). Use `docker compose
down` with your original overlays, **without `-v`**, so containers are removed but
their volumes remain. Back up your database and storage first.

Move your existing `.env` to the repository root (or your standalone deployment
folder). Move `deploy/secrets/` to `secrets/`, or update its host-path overrides.
Translate any optional variables using this table; unchanged values can stay:

| Old name | New name |
|---|---|
| `RUN_MIGRATIONS_ON_STARTUP` | `Database__RunMigrationsOnStartup` |
| `AUTH_REQUIRE_ADMIN_APPROVAL` | `Auth__RequireAdminApproval` |
| `AUTH_COOKIE_SAMESITE` | `Auth__CookieSameSite` |
| `AUTH_COOKIE_SECURE_POLICY` | `Auth__CookieSecurePolicy` |
| `FRONTEND_PUBLIC_URL` | `Cors__AllowedOrigins__0` |
| `HTTPS_REDIRECT` | `Https__Redirect` |
| `STORAGE_PROVIDER` | `Storage__Provider` |
| `STORAGE_PATH` | `Storage__Path` (see the storage note below) |
| `AZURE_BLOB_CONNECTION_STRING` | `Azure__BlobConnectionString` |
| `AZURE_BLOB_CONTAINER_NAME` | `Azure__BlobContainerName` |
| `LOG_PATH` | `Serilog__WriteTo__1__Args__path` |
| `ConnectionStrings__Redis` | `Redis__ConnectionString` for Compose |

Remove a former default proxy target of `http://luminapath-api:8080`; the new
service address is `http://api:8080`. Root installations that relied on migrations
being off must explicitly set `Database__RunMigrationsOnStartup=false`.

The project name stays `luminapath` and volume keys are unchanged, including
`luminapath_luminapath-postgres`, `luminapath_luminapath-storage` and
`luminapath_luminapath-redis`. If you previously used `-p` / `COMPOSE_PROJECT_NAME`,
keep that same project name. Service/container names change; scripts should use
`docker compose exec api ...` or `docker compose exec db ...`.

Start with `docker compose up -d` for published images, or use the development
overlay described in [CONTRIBUTING.md](CONTRIBUTING.md#full-stack-from-source).
For direct backend access without local builds, add a personal `compose.override.yml`:

```yaml
services:
  api:
    ports:
      - "127.0.0.1:8080:8080"
```

This replaces the former backend-port overlay. The development overlay already
exposes this port. Remove the old Compose files when migrating a standalone
installation; the repository no longer maintains a separate `deploy/` stack.

## Environment Variables

Only the three values in `.env.example` need filling in for the default stack.
Optional backend settings use native .NET names: `Auth:RequireAdminApproval`
becomes `Auth__RequireAdminApproval`. Add only the overrides you need to `.env`.

The API loads `.env` using Compose `env_file`; PostgreSQL, the frontend and tools
receive only their explicitly mapped settings. A shell variable alone is not
forwarded unless Compose explicitly maps it. Outside Compose, provide backend
settings through the process environment, user secrets or application JSON.
Environment variables override JSON configuration; built-in option defaults apply
when no value is configured. Some integration settings saved in the admin UI take
precedence over these startup defaults.

Compose owns container addresses, images, ports and mounts. The application owns
cookie, storage, logging and integration defaults. Explicit settings in a Compose
`environment` block override `env_file`, so container wiring has corresponding
interpolation overrides below. Recreate containers with `docker compose up -d`
after editing `.env`.

### Images, ports and profiles

| Variable | Default | When to change |
|---|---|---|
| `LUMINAPATH_VERSION` | `latest` | Pin both app images to one release tag. |
| `LUMINAPATH_API_IMAGE` | `sekijuo/luminapath-api:<version>` | Use a custom registry/image instead. |
| `LUMINAPATH_FRONTEND_IMAGE` | `sekijuo/luminapath-frontend:<version>` | Use a custom registry/image instead. |
| `FRONTEND_HTTP_PORT` | `4200` | Change the public frontend port. |
| `POSTGRES_VERSION` | `17-alpine` | Select a compatible Postgres image; major upgrades need a data migration. |
| `REDIS_VERSION` | `7-alpine` | Select the Redis image tag. |
| `BACKEND_HTTP_PORT` | `8080` | Development overlay only, bound to loopback. |
| `POSTGRES_PORT` | `5432` | Development host port only; the container still listens on 5432. |
| `REDIS_PORT` | `6379` | Development host port only. |
| `COMPOSE_PROFILES` | empty | Enable `tools`, `proxy`, or one GPU profile with the vLLM overlay. |
| `NGINX_HTTP_PORT` / `NGINX_HTTPS_PORT` | `80` / `443` | TLS proxy profile host ports. |

The development overlay always builds `luminapath-api:local` and
`luminapath-frontend:local`; release image/version overrides apply to deployment.

### Database, Redis and secret files

| Variable | Default | When to change |
|---|---|---|
| `POSTGRES_DB` / `POSTGRES_USER` | `luminapath` | Database name/user shared by API and Postgres initialization. Changing these does not rename an existing database/user. |
| `Database__Host` / `Database__Port` | `db` / `5432` | Point the API at another database server. |
| `Database__Name` / `Database__Username` | values above | Override the API's connection only. |
| `Database__Password` / `Database__PasswordFile` | `POSTGRES_PASSWORD` / `POSTGRES_PASSWORD_FILE` fallback | API-only database credentials or a mounted secret path. |
| `ConnectionStrings__Default` | empty | Complete connection string; overrides the individual database fields. |
| `Database__RunMigrationsOnStartup` | `true` in Docker; `false` outside Development otherwise | Disable when applying migrations separately. |
| `Redis__ConnectionString` | `redis:6379` in Docker; `localhost:6379` otherwise | External Redis or connection options. |
| `Redis__InstanceName` | `LuminaPath:` | Cache key prefix. |
| `POSTGRES_PASSWORD_FILE` | empty | Container path to an already-mounted database password file. |
| `LUMINAPATH_ADMIN_PASSWORD_FILE` | empty | Container path to an already-mounted admin password file. |
| `POSTGRES_PASSWORD_SECRET_FILE` | `./secrets/postgres_password.txt` | Host file used by the secrets overlay. |
| `LUMINAPATH_ADMIN_PASSWORD_SECRET_FILE` | `./secrets/admin_password.txt` | Host file used by the secrets overlay. |
| `Admin__Email` / `Admin__Password` / `Admin__PasswordFile` | bootstrap variables from `.env` | Native .NET alternatives for the API. |

File paths do not mount themselves: use the secrets overlay or arrange the mounts
in your orchestrator. Direct password values take precedence over password files.
`ConnectionStrings__Redis` remains a fallback outside Compose; use the canonical
`Redis__ConnectionString` with the provided stack.

### Auth, cookies and CORS

| Variable | Default | Notes |
|---|---|---|
| `Auth__RequireAdminApproval` | `true` | New self-registered users require admin activation. Development overrides this to `false`. |
| `Auth__CookieSameSite` | `Lax` | Use `None` only when browser cookie auth must cross sites. |
| `Auth__CookieSecurePolicy` | `Always` | Startup validation requires `Always` when SameSite is `None`. |
| `Cors__AllowedOrigins__0` | empty | Optional CORS origin. Leave empty when the frontend proxies `/api` same-origin. |
| `LUMINAPATH_CORS_ORIGINS` | empty | Semicolon- or comma-separated CORS origins as an alternative CORS list. |
| `FrontendUrls__0` | empty | Alternative .NET array form for CORS origins. |

Guardrails:

- CORS origins with `Auth__CookieSameSite=Lax` log a startup warning because
  cross-site cookie auth usually needs `None`.
- `Auth__CookieSameSite=None` with anything except
  `Auth__CookieSecurePolicy=Always` fails startup validation.

### Startup summary and warnings

On startup the backend logs a redacted configuration summary with safe values
only: environment, whether database/Redis are configured, storage provider,
migration switch, CORS origin count, cookie modes, admin-approval switch, AI
provider and scheduled-job switch.

It never logs passwords, connection strings, API keys or the bootstrap admin
email/password.

### HTTPS and reverse proxies

| Variable | Default | Notes |
|---|---|---|
| `Https__Redirect` | `false` (`true` in Development) | Enable only when the backend should redirect HTTP to HTTPS. |

#### Forwarded headers (`X-Forwarded-Proto` / `X-Forwarded-For`)

The backend honours `X-Forwarded-*` headers so HTTPS scheme detection and
per-IP rate limiting see the real client rather than the proxy hop. By
default it trusts those headers only when the immediate peer is on a
private network (RFC1918 + loopback + IPv6 ULA/link-local), which covers
the nginx → backend hop inside Docker. A directly internet-exposed backend
sees a public peer address outside that set, so spoofed headers are ignored.

Override the trust set when your proxy is on a non-private network or you
want to pin a specific proxy:

| Variable | Default | Notes |
|---|---|---|
| `ForwardedHeaders__KnownNetworks__0` | private ranges | CIDR allowlist; repeat the index for multiple entries. |
| `ForwardedHeaders__KnownProxies__0` | empty | Exact proxy IP allowlist; repeat the index for multiple entries. |
| `ForwardedHeaders__ForwardLimit` | `1` | Number of trusted proxy hops; raise for chained proxies. |

### Security headers

The backend always emits `X-Content-Type-Options`, `X-Frame-Options`,
`Referrer-Policy` and `Permissions-Policy`. It also emits a
Content-Security-Policy, which ships in **report-only** mode by default so a
mistuned policy surfaces as browser-console warnings instead of a broken UI.
Watch the console (or a `report-uri` collector) on the Blazor app, then flip
to enforcing once it is clean.

| Variable | Default | Notes |
|---|---|---|
| `SecurityHeaders__EnableContentSecurityPolicy` | `true` | Set false to drop the CSP header entirely. |
| `SecurityHeaders__ContentSecurityPolicyReportOnly` | `true` | Set false to enforce (send `Content-Security-Policy` instead of `…-Report-Only`). |
| `SecurityHeaders__ContentSecurityPolicy` | tuned default | Override the full policy string. The default allows same-origin scripts, inline styles (MudBlazor/Radzen) and Google Fonts. |
| `SecurityHeaders__ReportUri` | empty | Optional endpoint appended as a `report-uri` directive. |

### Email (SMTP)

Email delivery is **optional**. Without it, the app keeps a no-op sender that
shows account-confirmation links on-screen (development convenience), and
self-service password reset cannot deliver mail. Configure SMTP to turn on
real password-reset and email-confirmation messages.

| Variable | Default | Notes |
|---|---|---|
| `Email__Smtp__Host` | empty | SMTP server host. Setting this enables the real sender. |
| `Email__Smtp__Port` | `587` | SMTP port. |
| `Email__Smtp__User` | empty | SMTP username; omit for unauthenticated relays. |
| `Email__Smtp__Password` | empty | SMTP password. Prefer a secret/env over `appsettings.json`. |
| `Email__Smtp__UseStartTls` | `true` | STARTTLS (maps to `SmtpClient.EnableSsl`); the common port-587 setup. |
| `Email__FromAddress` | empty | Required once a host is set; must be a valid address. |
| `Email__FromName` | `LuminaPath` | Display name on outgoing mail. |

Startup validation fails fast if `Email__Smtp__Host` is set but
`Email__FromAddress` is missing/invalid or the port is out of range. Delivery
failures are logged but never surfaced to the requester, so a broken SMTP
relay can't be used to enumerate which email addresses have accounts.

Note: password reset requires the target account to have a confirmed email.
Admin-created and seeded users are confirmed automatically; self-registered
users confirm via the link sent once SMTP is configured.

### Storage

File-system storage is the default.

| Variable | Default | Notes |
|---|---|---|
| `Storage__Provider` | `FileSystem` | `Azure` for Azure Blob Storage or `GoogleDrive` for private Google Drive storage. |
| `Storage__Path` | `App_Data/storage` (under `/app` in Docker) | File-system storage path inside the API container. |
| `Azure__BlobConnectionString` | empty | Required when `Storage__Provider=Azure`. |
| `Azure__BlobContainerName` | `media` | Azure container name. |

Container mounts stay at `/app/App_Data/storage` and `/app/App_Data/logs`.
Normally leave these paths unchanged. If an existing install used `STORAGE_PATH`,
the same named volume can be mounted at the new default path without moving its
contents. Remove the old path override to use that default. If you need a custom
path, change both `Storage__Path` and the mount target in a personal Compose
override, and align `DatabaseBackup__Directory` with it.

| Variable | Default | When to change |
|---|---|---|
| `Serilog__WriteTo__1__Args__path` | `App_Data/logs/luminapath-.log` | File log destination; keep it within a persisted mount. |

### Google Drive storage

Google Drive can store uploaded covers/documents and copies of completed database
backups in your own account. PostgreSQL, Redis, logs and temporary files stay on
the VPS. The app keeps Drive files private and serves uploads through its existing
authenticated `/api/files/...` routes, so the app can remain VPN-only. The VPS
needs outbound HTTPS access to `oauth2.googleapis.com` and `www.googleapis.com`.

This provider is available in images built from this change. Build the source
with `compose.dev.yml` until a release containing it is published; setting the
provider on an older image will fail startup validation.

#### Connect from the backend (recommended)

1. Enable the Google Drive API in Google Cloud. Create an OAuth **Web application**
   client. A Desktop client or API key cannot be used for this browser flow.
2. Register the exact callback URL on that client, for example:
   `https://lumina.example.com/api/integrations/google-drive/callback`.
   For your VPN-only VPS, use an HTTPS hostname that your browser resolves/reaches
   through the VPN. The callback is a browser redirect; Google does not need
   inbound network access to your VPS. Google requires HTTPS and a valid hostname
   for web callbacks (not a private IP or `.local` name). Local development can use
   `http://localhost:4200/api/integrations/google-drive/callback` instead.
   See [Google's redirect URI rules](https://developers.google.com/identity/protocols/oauth2/web-server#uri-validation).
3. Add the OAuth client configuration to `.env`:

   ```dotenv
   GoogleDrive__ClientId=your-client-id.apps.googleusercontent.com
   GoogleDrive__ClientSecret=your-client-secret
   GoogleDrive__RedirectUri=https://lumina.example.com/api/integrations/google-drive/callback
   ```

   Protect `.env`. You can use `GoogleDrive__ClientSecretFile` with a mounted
   Docker secret instead of the inline secret (see the helper example below).
   Leave `GoogleDrive__RefreshToken` and `GoogleDrive__RefreshTokenFile` unset.
   Keep the current storage provider while connecting and preparing migration.
4. Recreate the API with `docker compose up -d`, sign in as an administrator, and
   open **Admin → Google Drive** at `/Admin/GoogleDrive` on the app's origin.
   Click **Connect Google Drive**, sign in to your storage account, and allow
   access. The page also links from backend Settings → Google Drive.
5. The backend saves an encrypted offline grant. With Compose, the grant and its
   encryption keys persist in the dedicated `luminapath-google-drive` volume,
   outside the uploaded-file folder. Keep this volume in your server backups and
   protect it: someone with both the grant and its keys can decrypt it. For a
   deployment outside Docker, persist `App_Data/google-drive` under the API's
   content root. No Python script, token copying, or API restart is needed after
   browser authorization. Reauthorization must use the same Google account and
   OAuth client to preserve existing file references.
6. For an empty installation, set `Storage__Provider=GoogleDrive` in `.env` and
   recreate the API. For existing filesystem uploads, follow the migration steps
   below before restarting normal traffic with the new provider. Connecting alone
   does not change the storage provider or move existing files.

The browser flow uses administrator-only endpoints, an antiforgery-protected
start, single-use state tied to the administrator (expires after ten minutes),
PKCE, and the `drive.file` scope. A restart during consent requires starting again.
When enforcing a custom backend CSP, allow `https://accounts.google.com` in
`form-action` for the authorization redirect; the built-in policy includes it.
Use **Reconnect Google Drive** if the grant is revoked or expired. Authorization
status reports a saved grant; it is not a continuous connectivity/expiry check.

#### Optional local authorization helper

The Python helper remains available for headless setups. Download a **Desktop app**
client JSON, or a **Web application** client JSON with
`http://127.0.0.1:8765/callback` registered. Run locally (Python 3.10+):

```sh
python3 scripts/authorize-google-drive.py --client-json /path/to/client.json
```

The helper saves `client_id.txt`, `client_secret.txt` and `refresh_token.txt` under
`secrets/google-drive/` with owner-only file permissions on Unix. Transfer them
securely to the VPS and keep the client JSON outside the repository/build context.
Configure `.env` using the contents of `client_id.txt` for the ID:

```dotenv
Storage__Provider=GoogleDrive
GoogleDrive__ClientId=your-client-id.apps.googleusercontent.com
GoogleDrive__ClientSecretFile=/run/secrets/google_drive_client_secret
GoogleDrive__RefreshTokenFile=/run/secrets/google_drive_refresh_token
```

Add to your personal `compose.override.yml` (merge with any VPN port override):

```yaml
services:
  api:
    secrets:
      - google_drive_client_secret
      - google_drive_refresh_token
secrets:
  google_drive_client_secret:
    file: ./secrets/google-drive/client_secret.txt
  google_drive_refresh_token:
    file: ./secrets/google-drive/refresh_token.txt
```

For browser setup with a file-based client secret, use only the
`google_drive_client_secret` entries; no refresh token file is needed.
Recreate the API with `docker compose up -d`. With explicit `-f` flags, include
`-f compose.override.yml`; Compose does not automatically add the override in that
case. Configured refresh tokens take precedence over browser grants, and the page
disables browser authorization until the external token setting is removed and
the API restarted. Reconnect the same account when moving from the helper to the
browser flow. Keep the same Web client; Desktop clients require the helper.

On first use the provider creates an app-marked `LuminaPath` folder in My Drive,
with separate `uploads` and `backups` subfolders. The `drive.file` grant covers
files created/authorized for this OAuth app; it does not grant access to arbitrary
pre-existing Drive files just because they are moved into a folder. Keep the same
OAuth client when reauthorizing. An external consent screen left in **Testing**
usually produces a Drive refresh token that expires after seven days; configure
its publishing status for ongoing use before authorizing. Revoked/expired tokens
require reauthorization. See [Google's scope guidance](https://developers.google.com/workspace/drive/api/guides/api-specific-auth)
and [OAuth token expiration](https://developers.google.com/identity/protocols/oauth2#expiration).

| Variable | Default | When to change |
|---|---|---|
| `GoogleDrive__ClientId` | empty | Required with the Drive provider. |
| `GoogleDrive__ClientSecret` / `GoogleDrive__ClientSecretFile` | empty | Client secret or mounted secret-file path. |
| `GoogleDrive__RefreshToken` / `GoogleDrive__RefreshTokenFile` | empty | Optional external grant from the helper; omit for browser authorization. |
| `GoogleDrive__RedirectUri` | empty | Exact registered Web client callback URL; required only for browser authorization. |
| `GoogleDrive__ConnectionPath` | `App_Data/google-drive` | Persistent directory for the encrypted browser grant and its keys. Mount this path if changed in Docker. |
| `GoogleDrive__FolderName` | `LuminaPath` | Root folder name; use distinct names for independent installations. |
| `GoogleDrive__FolderId` | empty (auto-create/find) | Existing root folder accessible to this OAuth app. Its marked subfolders are still isolated. |
| `GoogleDrive__UploadBackups` | `true` | Copy each newly completed database dump to Drive; set false for uploads only. |
| `GoogleDrive__CacheMinutes` | `5` | In-memory upload-download cache lifetime (0 disables, maximum 60). |

The cache is bounded to 32 MiB per API process and caches files up to 4 MiB.
Large transfers use temporary local disk and resumable, bounded-memory uploads.
Drive filenames are not unique: ambiguous duplicates cause an error instead of
selecting a file arbitrarily. Use a single API writer per Drive root; do not modify
its files manually. An interrupted upload can be retried; if completion was
uncertain, verify the existing file before retrying or removing it.

Backup jobs still create a local dump first. A failed Drive upload marks the job
failed and **retains the completed local dump**. Existing backup downloads and
local retention work as before. Remote backup copies are retained independently:
local cleanup and orphan-upload cleanup never delete Drive backups. Manage their
retention in Drive and download a dump from the `backups` folder for a normal
`pg_restore` recovery. Existing local backups are not automatically copied.

#### Migrating existing filesystem uploads

Changing providers does not automatically copy existing files. Stop the API and
frontend first so uploads and orphan cleanup cannot race with migration. Back up
your database and source volume. Configure Drive as above, then run a dry run
(the command can initialize the dedicated Drive folders, but does not copy files):

```sh
docker compose stop api frontend
docker compose run --rm --no-deps api --migrate-storage=/app/App_Data/storage --dry-run=true
```

Copy and verify the files:

```sh
docker compose run --rm --no-deps api --migrate-storage=/app/App_Data/storage --dry-run=false
docker compose up -d
```

Use the same Compose file list as your installation, including secret/VPN overrides.
The one-off command does not start the web server, run database migrations or run
background jobs. It preserves filenames so database references remain valid,
verifies SHA-256 after copying, skips identical existing files, and stops on a
conflicting file without overwriting it. Source files are never deleted. Only
files directly in the source directory are migrated; metadata sidecars,
subdirectories (including `backups`) and symbolic links are not copied. Symbolic
links cause an error. Keep the source volume until verification and backup are
complete. Azure-to-Drive migration requires exporting blobs with their original
names to a local directory first.

### Frontend runtime

| Variable | Default | Notes |
|---|---|---|
| `LUMINAPATH_API_ENDPOINT` | `/api` | Browser-facing API endpoint. Keep `/api` with the included frontend container. |
| `LUMINAPATH_API_PROXY_TARGET` | `http://api:8080` | Nginx target inside the Docker network. |
| `LUMINAPATH_CSP` | enforcing default | Full Content-Security-Policy for the served SPA. The frontend nginx also sends `X-Content-Type-Options`, `X-Frame-Options: DENY`, `Referrer-Policy` and `Permissions-Policy`. |

The frontend container serves the SPA with an **enforcing** CSP. The default
locks scripts to same-origin, allows inline styles (Angular/Ionic inject
`<style>` tags at runtime), permits `https:`/`data:` images for external
cover art, and keeps `connect-src 'self'` because the API is proxied
same-origin. If the SPA talks to a **different** API origin (custom
`LUMINAPATH_API_ENDPOINT`), override `LUMINAPATH_CSP` to add that origin to
`connect-src`, e.g.:

```text
LUMINAPATH_CSP=default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; connect-src 'self' https://api.example.com; frame-ancestors 'none'; object-src 'none'; base-uri 'self'
```

The frontend also proxies `/Account`, `/Admin`, `/_blazor`,
`/_content` and `/_framework` to the backend. This keeps the default deploy
path to a single public frontend port while preserving access to the Blazor
admin and account flows.

### AI provider settings

The backend supports Anthropic and OpenAI-compatible providers. The
OpenAI-compatible path covers Ollama, LM Studio, vLLM and OpenAI-style APIs.

| Variable | Default | Notes |
|---|---|---|
| `AiChat__Provider` | `anthropic` | Use `openai` for OpenAI-compatible endpoints. |
| `AiChat__MaxToolIterations` | `8` | Safety cap for tool-use loops. |
| `Anthropic__MaxTokens` | `4096` | Maximum output tokens. |
| `Anthropic__AnthropicVersion` | `2023-06-01` | API version header. |
| `ANTHROPIC_API_KEY` or `Anthropic__ApiKey` | empty | Required when using Anthropic. |
| `Anthropic__Model` | `claude-sonnet-4-6` | Anthropic model name. |
| `Anthropic__BaseUrl` | `https://api.anthropic.com` | Override only for compatible gateways. |
| `OPENAI_API_KEY` or `OpenAi__ApiKey` | empty | Optional for local endpoints. |
| `OPENAI_BASE_URL` or `OpenAi__BaseUrl` | `http://localhost:11434/v1` | OpenAI-compatible API root. |
| `OPENAI_MODEL` or `OpenAi__Model` | `llama3.2` | Model name. |
| `OpenAi__ToolChoice` | empty | Useful for vLLM auto tool calling. |

### Local vLLM overlay

Use these only with `compose.vllm.yml` and an `amd` or `nvidia` profile.

| Variable | Default | Notes |
|---|---|---|
| `COMPOSE_PROFILES` | empty | Set to `amd` or `nvidia`. |
| `VLLM_MODEL` | `NousResearch/Hermes-3-Llama-3.1-8B` | Model served by vLLM. |
| `VLLM_API_KEY` | `local-vllm` | API key shared between backend and vLLM. |
| `VLLM_PORT` | `8000` | Host port for vLLM. |
| `VLLM_GPU_MEMORY_UTILIZATION` | `0.90` | vLLM memory tuning. |
| `VLLM_TENSOR_PARALLEL_SIZE` | `1` | Multi-GPU tensor parallelism. |
| `VLLM_TOOL_CALL_PARSER` | `hermes` | Parser used for tool calls. |
| `HF_TOKEN` | empty | Optional Hugging Face token. |
| `HUGGING_FACE_HUB_TOKEN` | empty | Alternative Hugging Face token variable. |
| `VLLM_BASE_URL` | `http://vllm:8000/v1` | API-side endpoint. |
| `VLLM_TOOL_CHOICE` | `auto` | API-side tool choice. |
| `VLLM_DTYPE` | `auto` | Model data type. |
| `VLLM_MAX_MODEL_LEN` | `8192` | Model context length. |
| `VLLM_AMD_IMAGE` | `vllm/vllm-openai-rocm:latest` | AMD image override. |
| `VLLM_NVIDIA_IMAGE` | `vllm/vllm-openai:latest` | NVIDIA image override. |
| `VLLM_AMD_KFD_DEVICE` / `VLLM_AMD_DRI_DEVICE` | `/dev/kfd` / `/dev/dri` | AMD host device paths. |
| `VLLM_HIP_VISIBLE_DEVICES` / `VLLM_ROCR_VISIBLE_DEVICES` | `0` / `0` | AMD device selection. |
| `VLLM_NVIDIA_GPUS` / `VLLM_NVIDIA_VISIBLE_DEVICES` | `all` / `all` | NVIDIA GPU selection. |
| `VLLM_NVIDIA_DRIVER_CAPABILITIES` | `compute,utility` | NVIDIA container capabilities. |

### Metadata, imports and third-party APIs

These are optional. Empty credentials simply disable the provider features that
need them.

| Variable | Default | Notes |
|---|---|---|
| `GameMetadata__Provider` | `IgdbThenRawg` | Supported: `IgdbThenRawg`, `RawgThenIgdb`, `Igdb`, `Rawg`. |
| `GameMetadata__IgdbClientId` | empty | IGDB client id. |
| `GameMetadata__IgdbClientSecret` | empty | IGDB client secret. |
| `GameMetadata__RawgApiKey` | empty | RAWG API key. |
| `Steam__ApiBaseUrl` | `https://api.steampowered.com` | Override for tests or proxies. |
| `Steam__StoreBaseUrl` | `https://store.steampowered.com` | Override for tests or proxies. |
| `PSN__AuthorizationBaseUrl` | built in | Override only if Sony endpoints change. |
| `PSN__ProfileBaseUrl` | built in | Override only if Sony endpoints change. |
| `PSN__ApiBaseUrl` | built in | Override only if Sony endpoints change. |

### Google Calendar integration

Optional. When configured, users can link their Google account from the Angular
app's **Settings → Google Calendar** card or the Blazor **Account → Manage →
Google Calendar** page, and push their library's upcoming releases and dated
quests into a dedicated "LuminaPath" calendar with **Sync now**. Both surfaces
are hidden until the server is configured.

Operator setup: create a Google Cloud **OAuth 2.0 Web** client, enable the
**Google Calendar API**, and register the redirect URI
`https://<your-frontend-host>/api/integrations/google/callback`. The frontend
proxies `/api` to the backend, so use the public frontend origin. If users also
link from the Blazor admin on a different origin, add that origin's
`/api/integrations/google/callback` too (Google allows multiple redirect URIs),
or pin a single one with `GoogleCalendar__RedirectUri`.

The client id, client secret, and calendar name can be set **without an
environment variable or restart** from the Blazor admin **Settings → Google
Calendar** panel. Stored values are kept in `ApplicationSettings` (the secret as
plaintext, matching the IGDB client secret convention) and **override** the
environment variables below; the env vars remain as the fallback/default. The
integration is considered configured when an effective client id *and* secret
are present from either source.

| Variable | Default | Notes |
|---|---|---|
| `GOOGLE_CLIENT_ID` | empty | OAuth client id. Setting this (or the admin Settings panel) enables the integration. |
| `GOOGLE_CLIENT_SECRET` | empty | OAuth client secret (or `GoogleCalendar__ClientSecretFile`, or the admin Settings panel). |
| `GoogleCalendar__CalendarName` | `LuminaPath` | Name of the dedicated calendar created on first sync (also editable in the admin Settings panel). |
| `GoogleCalendar__RedirectUri` | derived from request | Override only if the auto-derived `{scheme}://{host}/api/integrations/google/callback` is wrong (e.g. local dev without the proxy). |
| `GoogleCalendar__SettingsReturnPath` | `/settings` | SPA path the callback returns to. |

Notes:

- Sync is one-way (LuminaPath → Google) and manual. It upserts events with
  stable ids (so re-syncing never duplicates) and removes events whose release
  or open quest no longer applies.
- The OAuth refresh token is encrypted at rest with ASP.NET Data Protection.
  In Docker the Data Protection key ring is container-local by default — if it
  isn't persisted, links become unreadable after a redeploy and users must
  reconnect. Persist the key ring (volume) for durable links.

### Background jobs and backups

| Variable | Default | Notes |
|---|---|---|
| `BackgroundJobs__ScheduledJobsEnabled` | `false` | Master switch for scheduled jobs. Admins can still trigger ad-hoc jobs. |
| `BackgroundJobs__BackupIntervalHours` | `24` | Scheduled backup cadence. |
| `BackgroundJobs__BackupRetentionCount` | `10` | Number of backups to keep. |
| `BackgroundJobs__JobHistoryRetentionDays` | `30` | Job record retention. |
| `BackgroundJobs__MaintenanceIntervalHours` | `6` | Maintenance cleanup cadence. |
| `BackgroundJobs__OrphanedBlobCleanupIntervalHours` | `24` | Set `0` to disable this scheduled job only. |
| `DatabaseBackup__Directory` | `App_Data/storage/backups` | Backup output directory. |
| `DatabaseBackup__PgDumpPath` | `pg_dump` | Path to `pg_dump`. |
| `DatabaseBackup__FilePrefix` | `luminapath` | Backup file prefix. |
| `DatabaseBackup__MaxListedBackups` | `20` | Maximum backups listed in admin UI. |

### Optional tools

Use these only with the `tools` profile.

| Variable | Default | Notes |
|---|---|---|
| `PGADMIN_VERSION` | `latest` | pgAdmin image tag. |
| `PGADMIN_HTTP_PORT` | `5050` | Host port for pgAdmin. |
| `PGADMIN_DEFAULT_EMAIL` | `admin@example.com` | Local tool login. Change it if exposed. |
| `PGADMIN_DEFAULT_PASSWORD` | `admin` | Local tool login. Change it if exposed. |

## AI Assistant

Choose the provider and optional credentials from the reference above. For a
containerized API, `localhost` refers to its own container; use a reachable
server address for Ollama or LM Studio. The vLLM overlay wires this automatically.

### Built-in database tools


All tools are scoped to the authenticated user, so the agent can only ever see or change the signed-in user's own data:

| Tool | Access | Purpose |
| --- | --- | --- |
| `list_upcoming_releases` | read | Future game releases, optional month/year filter, optional `only_my_library` |
| `search_my_library` | read | Search games in the user's library by name fragment, status or platform |
| `library_summary` | read | Counts by status, total logged hours, estimated backlog hours |
| `my_quests` | read | Quests for the user, filterable by `open` / `completed` / `all` |
| `my_gaming_sessions` | read | Scheduled sessions in a date window (default: next 14 days) |
| `create_quest` | write | Create a quest on the user's board. The agent is instructed to confirm the details with the user before calling it. |

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

## Development and Releases

Development setup, checks and Android build instructions are in
[CONTRIBUTING.md](CONTRIBUTING.md). The .NET project boundaries remain unchanged:
Core, Infrastructure, the API/Blazor host, the Angular/Ionic frontend and tests.
See [architecture](docs/02-architecture/index.md) for their responsibilities.

## CI/CD

- CI builds and tests the backend and frontend, checks every Compose variant and
  builds both container images.
- Docs validates the documentation site.
- Release publishes matching Docker Hub images, an Android APK, source archives,
  license notices and checksums for a version tag.
- Manual GHCR and Azure Release publishes to GHCR and optionally deploys the API
  to Azure.

See the [release runbook](docs/05-runbooks/releases.md) for publishing instructions,
required credentials, Android signing and source/license requirements.

## License

For contribution instructions, see [CONTRIBUTING.md](CONTRIBUTING.md).
To report a vulnerability, see [SECURITY.md](SECURITY.md).

Copyright (C) 2023-2026 dominictosku.

Unless otherwise noted, LuminaPath's original source code and documentation are
licensed under the **GNU Affero General Public License, version 3 only**
(`AGPL-3.0-only`). See [LICENSE](LICENSE) for the complete terms.

You may use, modify, and distribute LuminaPath, including commercially, under
those terms. When distributing covered versions, you must preserve the license
and provide corresponding source using one of the methods it permits. Modified
versions offered over a network must prominently offer their corresponding
source to those users, as specified in section 13 of the license.

LuminaPath is distributed without any warranty, including the implied warranties
of merchantability or fitness for a particular purpose. See the license for details.

Dependencies and bundled third-party material retain their own licenses and
copyright notices; the project license does not replace those terms.
See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) for the inventory and notices.
