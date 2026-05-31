# Configuration reference

LuminaPath keeps the first-run `.env` intentionally small. For a normal Docker
start, set only:

```text
POSTGRES_PASSWORD=your-strong-database-password
LUMINAPATH_ADMIN_EMAIL=you@example.com
LUMINAPATH_ADMIN_PASSWORD=your-strong-admin-password
```

For file-based secrets, use `deploy/docker-compose.secrets.yml` and provide
the password files instead of putting the password values in `.env`.

Everything below is optional and has a default in Docker Compose or in the
backend option classes.

## How settings are read

The backend reads configuration in this order:

1. Environment variables
2. `appsettings.{Environment}.json`
3. `appsettings.json`

Nested .NET keys use double underscores as environment variables. For example,
`Auth:CookieSameSite` becomes `Auth__CookieSameSite`.

Docker Compose accepts friendlier `.env` variables and maps them to the backend
keys where needed.

## Required first-run values

| Variable | Used by | Default | Notes |
|---|---|---|---|
| `POSTGRES_PASSWORD` | Docker Postgres and backend connection string | none | Required unless you use `POSTGRES_PASSWORD_FILE` or the secrets overlay. |
| `LUMINAPATH_ADMIN_EMAIL` | Bootstrap admin seed | none | Required for non-development startup. Used only when no admin exists. |
| `LUMINAPATH_ADMIN_PASSWORD` | Bootstrap admin seed | none | Required unless you use `LUMINAPATH_ADMIN_PASSWORD_FILE` or the secrets overlay. Must satisfy the production password policy. |
| `POSTGRES_PASSWORD_FILE` | Docker Postgres and backend connection string | empty | Container path to a mounted password file. |
| `LUMINAPATH_ADMIN_PASSWORD_FILE` | Bootstrap admin seed | empty | Container path to a mounted admin password file. |

## Docker image and port overrides

| Variable | Default | Notes |
|---|---|---|
| `LUMINAPATH_API_IMAGE` | Root: `luminapath-api:local`; deploy: `sekijuo/luminapath-api:latest` | Pin this to a release tag for stable installs. |
| `LUMINAPATH_FRONTEND_IMAGE` | Root: `luminapath-frontend:local`; deploy: `sekijuo/luminapath-frontend:latest` | Pin this to the same release tag as the API. |
| `BACKEND_HTTP_PORT` | `8080` | Host port for the API and Blazor admin. Deploy uses this only with `deploy/docker-compose.backend.yml`. |
| `BACKEND_HTTP_BIND` | `127.0.0.1` | Deploy backend-port overlay bind address. Set `0.0.0.0` only when intentionally exposing the backend port. |
| `FRONTEND_HTTP_PORT` | `4200` | Host port for the Angular frontend container. |
| `POSTGRES_PORT` | `5432` | Root development compose only. Bound to `127.0.0.1`. |
| `REDIS_PORT` | `6379` | Root development compose only. Bound to `127.0.0.1`. |
| `POSTGRES_VERSION` | `17-alpine` | Postgres image tag. |
| `REDIS_VERSION` | `7-alpine` | Redis image tag. |

## Database and migrations

| Variable | Default | Notes |
|---|---|---|
| `POSTGRES_DB` | `luminapath` | Docker database name. |
| `POSTGRES_USER` | `luminapath` | Docker database user. |
| `RUN_MIGRATIONS_ON_STARTUP` | Root: `false`; deploy: `true` | Convenient for self-hosted installs. Public production deployments should review migrations deliberately. |
| `ConnectionStrings__Default` | empty | Use this directly only outside the provided Compose files; otherwise the app builds it from the `Database__*` settings. |
| `Database__Host` | Set by Compose | Database host when not using `ConnectionStrings__Default`. |
| `Database__Port` | Set by Compose | Database port when not using `ConnectionStrings__Default`. |
| `Database__Name` | Set by Compose | Database name when not using `ConnectionStrings__Default`. |
| `Database__Username` | Set by Compose | Database user when not using `ConnectionStrings__Default`. |
| `Database__Password` | `POSTGRES_PASSWORD` | Database password value. Prefer `Database__PasswordFile` for mounted secrets. |
| `Database__PasswordFile` | `POSTGRES_PASSWORD_FILE` | Container path to a mounted database password file. |
| `ConnectionStrings__Redis` | Set by Compose | Use this directly only outside the provided Compose files. |
| `Redis__ConnectionString` | `localhost:6379` | Backend Redis cache connection outside Compose. |
| `Redis__InstanceName` | `LuminaPath:` | Prefix for cache keys. |

## File-based secrets

The deploy folder includes `docker-compose.secrets.yml` for local Docker
Compose secrets. Put the secrets in files that are not committed:

```text
deploy/secrets/postgres_password.txt
deploy/secrets/admin_password.txt
```

Then keep `POSTGRES_PASSWORD` and `LUMINAPATH_ADMIN_PASSWORD` blank in `.env`
and start with:

```powershell
docker compose --env-file .env -f docker-compose.yml -f docker-compose.secrets.yml up -d
```

You can override the host file paths with:

```text
POSTGRES_PASSWORD_SECRET_FILE=./secrets/postgres_password.txt
LUMINAPATH_ADMIN_PASSWORD_SECRET_FILE=./secrets/admin_password.txt
```

For orchestrators that mount secrets themselves, set container-path variables
directly:

```text
POSTGRES_PASSWORD_FILE=/run/secrets/luminapath_postgres_password
LUMINAPATH_ADMIN_PASSWORD_FILE=/run/secrets/luminapath_admin_password
```

## Auth, cookies and CORS

| Variable | Default | Notes |
|---|---|---|
| `AUTH_REQUIRE_ADMIN_APPROVAL` | `true` | New self-registered users require admin activation. Development overrides this to `false`. |
| `AUTH_COOKIE_SAMESITE` | `Lax` | Use `None` only when browser cookie auth must cross sites. |
| `AUTH_COOKIE_SECURE_POLICY` | `Always` | Startup validation requires `Always` when SameSite is `None`. |
| `FRONTEND_PUBLIC_URL` | empty | Optional CORS origin. Leave empty when the frontend proxies `/api` same-origin. |
| `LUMINAPATH_CORS_ORIGINS` | empty | Semicolon- or comma-separated CORS origins for non-Docker hosting. |
| `FrontendUrls__0` | empty | Alternative .NET array form for CORS origins. |

Guardrails:

- CORS origins with `AUTH_COOKIE_SAMESITE=Lax` log a startup warning because
  cross-site cookie auth usually needs `None`.
- `AUTH_COOKIE_SAMESITE=None` with anything except
  `AUTH_COOKIE_SECURE_POLICY=Always` fails startup validation.

## Startup summary and warnings

On startup the backend logs a redacted configuration summary with safe values
only: environment, whether database/Redis are configured, storage provider,
migration switch, CORS origin count, cookie modes, admin-approval switch, AI
provider and scheduled-job switch.

It never logs passwords, connection strings, API keys or the bootstrap admin
email/password.

## HTTPS and reverse proxies

| Variable | Default | Notes |
|---|---|---|
| `HTTPS_REDIRECT` | `false` in Compose | Keep false when TLS terminates at a reverse proxy and the backend receives plain HTTP internally. |
| `Https__Redirect` | Production default is on unless explicitly disabled | Raw .NET key outside Compose. |

### Forwarded headers (`X-Forwarded-Proto` / `X-Forwarded-For`)

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

## Security headers

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

## Email (SMTP)

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

## Storage

File-system storage is the default.

| Variable | Default | Notes |
|---|---|---|
| `STORAGE_PROVIDER` | `FileSystem` | Set to `Azure` to use Azure Blob Storage. |
| `STORAGE_PATH` | `/app/App_Data/storage` in Compose | File-system storage path inside the API container. |
| `AZURE_BLOB_CONNECTION_STRING` | empty | Required when `STORAGE_PROVIDER=Azure`. |
| `AZURE_BLOB_CONTAINER_NAME` | `media` | Azure container name. |

Raw .NET equivalents:

```text
Storage__Provider=FileSystem
Storage__Path=App_Data/storage
Azure__BlobConnectionString=...
Azure__BlobContainerName=media
```

## Frontend runtime

| Variable | Default | Notes |
|---|---|---|
| `LUMINAPATH_API_ENDPOINT` | `/api` | Browser-facing API endpoint. Keep `/api` with the included frontend container. |
| `LUMINAPATH_API_PROXY_TARGET` | Root: `http://api:8080`; deploy: `http://luminapath-api:8080` | Nginx target inside the Docker network. |
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

The deploy frontend also proxies `/Account`, `/Admin`, `/_blazor`,
`/_content` and `/_framework` to the backend. This keeps the default deploy
path to a single public frontend port while preserving access to the Blazor
admin and account flows.

## AI assistant

The backend supports Anthropic and OpenAI-compatible providers. The
OpenAI-compatible path covers Ollama, LM Studio, vLLM and OpenAI-style APIs.

| Variable | Default | Notes |
|---|---|---|
| `AiChat__Provider` | `anthropic` | Use `openai` for OpenAI-compatible endpoints. |
| `AiChat__MaxToolIterations` | `8` | Safety cap for tool-use loops. |
| `ANTHROPIC_API_KEY` or `Anthropic__ApiKey` | empty | Required when using Anthropic. |
| `Anthropic__Model` | `claude-sonnet-4-6` | Anthropic model name. |
| `Anthropic__BaseUrl` | `https://api.anthropic.com` | Override only for compatible gateways. |
| `OPENAI_API_KEY` or `OpenAi__ApiKey` | empty | Optional for local endpoints. |
| `OPENAI_BASE_URL` or `OpenAi__BaseUrl` | `http://localhost:11434/v1` | OpenAI-compatible API root. |
| `OPENAI_MODEL` or `OpenAi__Model` | `llama3.2` | Model name. |
| `OpenAi__ToolChoice` | empty | Useful for vLLM auto tool calling. |

## Local vLLM overlay

Use these only with `docker-compose.vllm.yml` and an `amd` or `nvidia` profile.

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

## Metadata, imports and third-party APIs

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

## Google Calendar integration

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

| Variable | Default | Notes |
|---|---|---|
| `GOOGLE_CLIENT_ID` | empty | OAuth client id. Setting this enables the integration. |
| `GOOGLE_CLIENT_SECRET` | empty | OAuth client secret (or `GoogleCalendar__ClientSecretFile`). |
| `GoogleCalendar__CalendarName` | `LuminaPath` | Name of the dedicated calendar created on first sync. |
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

## Background jobs and backups

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

## Optional tools

Use these only with `docker-compose.tools.yml`.

| Variable | Default | Notes |
|---|---|---|
| `PGADMIN_VERSION` | `latest` | pgAdmin image tag. |
| `PGADMIN_HTTP_PORT` | `5050` | Host port for pgAdmin. |
| `PGADMIN_DEFAULT_EMAIL` | `admin@example.com` | Local tool login. Change it if exposed. |
| `PGADMIN_DEFAULT_PASSWORD` | `admin` | Local tool login. Change it if exposed. |
