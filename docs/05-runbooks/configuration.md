# Configuration reference

LuminaPath keeps the first-run `.env` intentionally small. For a normal Docker
start, set only:

```text
POSTGRES_PASSWORD=your-strong-database-password
LUMINAPATH_ADMIN_EMAIL=you@example.com
LUMINAPATH_ADMIN_PASSWORD=your-strong-admin-password
```

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
| `POSTGRES_PASSWORD` | Docker Postgres and backend connection string | none | Required. Compose refuses to start without it. |
| `LUMINAPATH_ADMIN_EMAIL` | Bootstrap admin seed | none | Required for non-development startup. Used only when no admin exists. |
| `LUMINAPATH_ADMIN_PASSWORD` | Bootstrap admin seed | none | Required for non-development startup. Must satisfy the production password policy. |

## Docker image and port overrides

| Variable | Default | Notes |
|---|---|---|
| `LUMINAPATH_API_IMAGE` | Root: `luminapath-api:local`; deploy: `dominictosku/luminapath-api:latest` | Pin this to a release tag for stable installs. |
| `LUMINAPATH_FRONTEND_IMAGE` | Root: `luminapath-frontend:local`; deploy: `dominictosku/luminapath-frontend:latest` | Pin this to the same release tag as the API. |
| `BACKEND_HTTP_PORT` | `8080` | Host port for the API and Blazor admin. |
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
| `ConnectionStrings__Default` | Set by Compose | Use this directly only outside the provided Compose files. |
| `ConnectionStrings__Redis` | Set by Compose | Use this directly only outside the provided Compose files. |
| `Redis__ConnectionString` | `localhost:6379` | Backend Redis cache connection outside Compose. |
| `Redis__InstanceName` | `LuminaPath:` | Prefix for cache keys. |

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

## HTTPS and reverse proxies

| Variable | Default | Notes |
|---|---|---|
| `HTTPS_REDIRECT` | `false` in Compose | Keep false when TLS terminates at a reverse proxy and the backend receives plain HTTP internally. |
| `Https__Redirect` | Production default is on unless explicitly disabled | Raw .NET key outside Compose. |

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
