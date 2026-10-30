# Configuration Reference

This document provides a comprehensive reference for configuring LuminaPath.

LuminaPath uses native .NET configuration hierarchy: settings can be provided via environment variables, `appsettings.json`, command-line arguments, or Docker secrets. In Docker environments, `.env` is loaded automatically by Docker Compose.

---

## Configuration Basics & Syntax

- **Nested Keys**: In Linux and Docker environments, nested .NET configuration paths use double underscores (`__`). For example, `Auth:RequireAdminApproval` becomes `Auth__RequireAdminApproval`.
- **Precedence**: Environment variables override `appsettings.json`. Certain integration settings configured via the Blazor Admin UI (`/Admin/Settings`) take precedence over environment variables.
- **Redaction**: On startup, the backend outputs a sanitized configuration summary to logs. Passwords, tokens, API keys, and database connection strings are never logged.

---

## Required Baseline Variables

For a standard Docker Compose deployment, only these three variables are required in `.env`:

```dotenv
POSTGRES_PASSWORD=your-strong-database-password
LUMINAPATH_ADMIN_EMAIL=you@example.com
LUMINAPATH_ADMIN_PASSWORD=your-strong-admin-password
```

| Variable | Requirement | Description |
|---|---|---|
| `POSTGRES_PASSWORD` | Required | Password for the PostgreSQL `luminapath` user. |
| `LUMINAPATH_ADMIN_EMAIL` | Required | Email address for the initial seeded administrator account. |
| `LUMINAPATH_ADMIN_PASSWORD` | Required | Password for the initial admin account (min 10 chars, upper, lower, digit, symbol). |

---

## Docker Stack, Images & Ports

| Variable | Default | Description |
|---|---|---|
| `LUMINAPATH_VERSION` | `latest` | Release tag for API and frontend container images. |
| `LUMINAPATH_API_IMAGE` | `sekijuo/luminapath-api:<version>` | Container image for the backend API. |
| `LUMINAPATH_FRONTEND_IMAGE` | `sekijuo/luminapath-frontend:<version>` | Container image for the Angular/Ionic frontend. |
| `FRONTEND_HTTP_PORT` | `4200` | Public host port for the frontend web application. |
| `POSTGRES_VERSION` | `17-alpine` | PostgreSQL container image tag. |
| `REDIS_VERSION` | `7-alpine` | Redis container image tag. |
| `BACKEND_HTTP_PORT` | `8080` | Port for backend container (development overlay only). |
| `POSTGRES_PORT` | `5432` | Exposed PostgreSQL port on host (development overlay only). |
| `REDIS_PORT` | `6379` | Exposed Redis port on host (development overlay only). |
| `COMPOSE_PROFILES` | empty | Active Docker Compose profiles: `tools`, `proxy`, `amd`, or `nvidia`. |
| `NGINX_HTTP_PORT` / `NGINX_HTTPS_PORT` | `80` / `443` | Host ports for the optional Nginx TLS reverse proxy profile. |

---

## Database, Redis & Secrets

| Variable | Default | Description |
|---|---|---|
| `POSTGRES_DB` / `POSTGRES_USER` | `luminapath` | Database name and username used by PostgreSQL container. |
| `Database__Host` / `Database__Port` | `db` / `5432` | Host and port to reach PostgreSQL from the API. |
| `Database__Name` / `Database__Username` | `luminapath` | Database name and user for EF Core connection. |
| `Database__Password` | `${POSTGRES_PASSWORD}` | Database password for the API. |
| `Database__PasswordFile` | empty | Container path to mounted password secret file. |
| `ConnectionStrings__Default` | empty | Full connection string; overrides individual database fields. |
| `Database__RunMigrationsOnStartup` | `true` in Docker | Automatically apply pending EF Core migrations on startup. Set `false` for manual SQL deployments. |
| `Redis__ConnectionString` | `redis:6379` in Docker | Redis cache host and port. |
| `Redis__InstanceName` | `LuminaPath:` | Key prefix for cached items. |
| `POSTGRES_PASSWORD_SECRET_FILE` | `./secrets/postgres_password.txt` | Host path for database password with `compose.secrets.yml`. |
| `LUMINAPATH_ADMIN_PASSWORD_SECRET_FILE` | `./secrets/admin_password.txt` | Host path for admin password with `compose.secrets.yml`. |

---

## Authentication, Cookies & CORS

| Variable | Default | Description |
|---|---|---|
| `Auth__RequireAdminApproval` | `true` | If `true`, newly registered users cannot log in until approved by an administrator in the Admin UI. |
| `Auth__CookieSameSite` | `Lax` | SameSite mode for auth cookies (`Lax`, `Strict`, `None`). Use `None` only for cross-origin setups. |
| `Auth__CookieSecurePolicy` | `Always` | Requires HTTPS for auth cookies. Mandatory when `SameSite=None`. |
| `Cors__AllowedOrigins__0` | empty | Allowed CORS origin (e.g. `https://my-app.example.com`). Leave empty when proxied same-origin. |
| `LUMINAPATH_CORS_ORIGINS` | empty | Comma- or semicolon-separated alternative for CORS origins. |

---

## Reverse Proxy & Forwarded Headers

When running behind reverse proxies (Nginx, Caddy, Traefik, Cloudflare), the backend inspects `X-Forwarded-*` headers to determine the client's actual IP and HTTPS scheme.

| Variable | Default | Description |
|---|---|---|
| `Https__Redirect` | `false` (`true` in Dev) | Forces redirect from HTTP to HTTPS within the ASP.NET pipeline. |
| `ForwardedHeaders__KnownNetworks__0` | Private ranges | CIDR allowlist for trusted proxies. Defaults to RFC1918, loopback, and IPv6 local. |
| `ForwardedHeaders__KnownProxies__0` | empty | Specific proxy IP allowlist. |
| `ForwardedHeaders__ForwardLimit` | `1` | Number of proxy hops to parse. |

---

## Security Headers & Content Security Policy (CSP)

The backend sends security headers (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Permissions-Policy`, and CSP).

| Variable | Default | Description |
|---|---|---|
| `SecurityHeaders__EnableContentSecurityPolicy` | `true` | Enables or disables CSP header emission. |
| `SecurityHeaders__ContentSecurityPolicyReportOnly` | `true` | Default is report-only mode (`Content-Security-Policy-Report-Only`) so unexpected violations don't break UI. Set to `false` to enforce. |
| `SecurityHeaders__ContentSecurityPolicy` | Tuned default | Custom policy string overriding defaults. |
| `SecurityHeaders__ReportUri` | empty | URL to receive violation reports. |

---

## Email Delivery (SMTP)

Email delivery is optional. If unconfigured, registration and password reset links are logged or displayed in development mode.

| Variable | Default | Description |
|---|---|---|
| `Email__Smtp__Host` | empty | SMTP server hostname. Setting this enables real email delivery. |
| `Email__Smtp__Port` | `587` | SMTP port (typically `587` for STARTTLS, `465` for SSL). |
| `Email__Smtp__User` | empty | SMTP authentication username. |
| `Email__Smtp__Password` | empty | SMTP authentication password. |
| `Email__Smtp__UseStartTls` | `true` | Enable STARTTLS encryption. |
| `Email__FromAddress` | empty | Sender email address (required if host is configured). |
| `Email__FromName` | `LuminaPath` | Display name on outgoing emails. |

---

## Storage & File Management

For complete details on S3, Google Drive, and Azure Blob setups, see the [Storage Providers Runbook](storage-providers.md).

| Variable | Default | Description |
|---|---|---|
| `Storage__Provider` | `FileSystem` | Storage provider: `FileSystem`, `S3`, `GoogleDrive`, or `Azure`. |
| `Storage__FallbackProvider` | empty | Secondary provider to check during provider migration for zero-downtime cutover. |
| `Storage__Path` | `App_Data/storage` | Local directory path for `FileSystem` uploads. |
| `Azure__BlobConnectionString` | empty | Azure Blob connection string. |
| `Azure__BlobContainerName` | `media` | Azure container name. |
| `S3__*` | *(see runbook)* | Amazon S3 and S3-compatible configuration. |
| `GoogleDrive__*` | *(see runbook)* | Google Drive OAuth and folder settings. |

---

## Frontend Runtime Configuration

| Variable | Default | Description |
|---|---|---|
| `LUMINAPATH_API_ENDPOINT` | `/api` | Public base URL the frontend uses to call the API. |
| `LUMINAPATH_API_PROXY_TARGET` | `http://api:8080` | Internal backend URL targeted by the frontend's Nginx proxy. |
| `LUMINAPATH_CSP` | Enforcing default | Content Security Policy sent by the frontend Nginx web server. |

---

## AI Assistant & LLM Integration

LuminaPath features an AI chat assistant with database tools and Model Context Protocol (MCP) server support. See the [AI Assistant Feature Guide](../03-features/ai-assistant.md).

| Variable | Default | Description |
|---|---|---|
| `AiChat__Provider` | `anthropic` | `anthropic` or `openai` (for OpenAI, Ollama, LM Studio, vLLM). |
| `AiChat__MaxToolIterations` | `8` | Maximum tool execution loops per turn. |
| `ANTHROPIC_API_KEY` or `Anthropic__ApiKey` | empty | Anthropic API key. |
| `Anthropic__Model` | `claude-sonnet-4-6` | Claude model name. |
| `Anthropic__BaseUrl` | `https://api.anthropic.com` | Custom Anthropic API endpoint. |
| `Anthropic__MaxTokens` | `4096` | Output token limit. |
| `OPENAI_API_KEY` or `OpenAi__ApiKey` | empty | API key for OpenAI or compatible endpoints. |
| `OPENAI_BASE_URL` or `OpenAi__BaseUrl` | `http://localhost:11434/v1` | Base URL for OpenAI-compatible endpoint (Ollama default shown). |
| `OPENAI_MODEL` or `OpenAi__Model` | `llama3.2` | Model identifier. |
| `OpenAi__ToolChoice` | empty | Optional tool choice parameter (e.g. `auto`). |

### Local vLLM Overlay Variables

Used with `compose.vllm.yml` and `--profile amd` or `--profile nvidia`:

| Variable | Default | Description |
|---|---|---|
| `VLLM_MODEL` | `NousResearch/Hermes-3-Llama-3.1-8B` | Model name pulled and served by vLLM. |
| `VLLM_API_KEY` | `local-vllm` | Shared API token between API and vLLM. |
| `VLLM_PORT` | `8000` | Host port for vLLM API. |
| `VLLM_GPU_MEMORY_UTILIZATION` | `0.90` | GPU VRAM utilization ratio. |
| `VLLM_TOOL_CALL_PARSER` | `hermes` | Tool call parsing grammar (e.g. `hermes`, `llama3_json`). |
| `HF_TOKEN` | empty | Hugging Face token for gated models. |

---

## Metadata Providers & Integrations

See [Third-Party Integrations](../03-features/integrations.md) for detailed setup.

| Variable | Default | Description |
|---|---|---|
| `GameMetadata__Provider` | `IgdbThenRawg` | Order of game metadata lookup: `IgdbThenRawg`, `RawgThenIgdb`, `Igdb`, `Rawg`. |
| `GameMetadata__IgdbClientId` | empty | Twitch/IGDB Developer Client ID. |
| `GameMetadata__IgdbClientSecret` | empty | Twitch/IGDB Developer Client Secret. |
| `GameMetadata__RawgApiKey` | empty | RAWG API Key. |
| `Steam__ApiBaseUrl` | `https://api.steampowered.com` | Steam Web API base URL. |
| `GOOGLE_CLIENT_ID` | empty | Google OAuth Client ID for Calendar sync (can also be set in Admin UI). |
| `GOOGLE_CLIENT_SECRET` | empty | Google OAuth Client Secret for Calendar sync. |
| `GoogleCalendar__CalendarName` | `LuminaPath` | Name of Google Calendar created for releases & quests. |

---

## Scheduled Background Jobs & Backups

| Variable | Default | Description |
|---|---|---|
| `BackgroundJobs__ScheduledJobsEnabled` | `false` | Master switch for recurring background tasks. |
| `BackgroundJobs__BackupIntervalHours` | `24` | Interval for automated database dumps. |
| `BackgroundJobs__BackupRetentionCount` | `10` | Number of automated database dumps to retain. |
| `BackgroundJobs__JobHistoryRetentionDays` | `30` | Days to retain execution records for background jobs. |
| `BackgroundJobs__MaintenanceIntervalHours` | `6` | Interval for general database maintenance. |
| `BackgroundJobs__OrphanedBlobCleanupIntervalHours` | `24` | Interval for cleaning orphaned storage files (`0` disables). |
| `DatabaseBackup__Directory` | `App_Data/storage/backups` | Output path inside container for `pg_dump` backups. |
| `DatabaseBackup__PgDumpPath` | `pg_dump` | Path to `pg_dump` binary. |
| `DatabaseBackup__FilePrefix` | `luminapath` | File prefix for dump archives. |

---

## Developer & Management Tools

Active when using `--profile tools`:

| Variable | Default | Description |
|---|---|---|
| `PGADMIN_VERSION` | `latest` | pgAdmin container image tag. |
| `PGADMIN_HTTP_PORT` | `5050` | Host port for pgAdmin web console (<http://localhost:5050>). |
| `PGADMIN_DEFAULT_EMAIL` | `admin@example.com` | Login email for pgAdmin. |
| `PGADMIN_DEFAULT_PASSWORD` | `admin` | Login password for pgAdmin. |
