# LuminaPath Docker Hub Deployment

This folder is for users who want to run LuminaPath from prebuilt Docker Hub images instead of building the repository locally.

## Start

Copy the example environment file:

```powershell
Copy-Item .env.example .env
```

Edit `.env` and set the required first-run values:

```text
POSTGRES_PASSWORD=your-strong-password
LUMINAPATH_ADMIN_EMAIL=you@example.com
LUMINAPATH_ADMIN_PASSWORD=your-strong-admin-password
```

The full optional reference is in [`../docs/05-runbooks/configuration.md`](../docs/05-runbooks/configuration.md).

Start LuminaPath:

```powershell
docker compose --env-file .env up -d
```

Open:

```text
http://localhost:4200
```

The frontend container proxies `/api` to the backend container internally.
PostgreSQL and Redis are only reachable on the Docker network.

The backend API is exposed at:

```text
http://localhost:8080
```

## Homelab Domains

The included frontend container uses `/api` and proxies requests to the
backend container, so you usually do not need CORS configuration.

When the frontend and API are served through the same public origin, keep the
default cookie settings:

```text
AUTH_COOKIE_SAMESITE=Lax
AUTH_COOKIE_SECURE_POLICY=Always
```

Keep this value when using the included frontend container:

```text
LUMINAPATH_API_ENDPOINT=/api
LUMINAPATH_API_PROXY_TARGET=http://luminapath-api:8080
```

The frontend container proxies `/api` to the backend container internally.

Only add a CORS origin when you intentionally serve the frontend from a
different origin than the API. If you do that, set the exact HTTPS frontend
origin and allow cross-site cookies:

```text
FRONTEND_PUBLIC_URL=https://luminapath.yourdomain.com
AUTH_COOKIE_SAMESITE=None
AUTH_COOKIE_SECURE_POLICY=Always
```

## Updating

Pull the newest images and restart:

```powershell
docker compose --env-file .env pull
docker compose --env-file .env up -d
```

## Pinning a Version

For stable homelab installs, pin a release version instead of `latest`:

```text
LUMINAPATH_API_IMAGE=dominictosku/luminapath-api:1.2.3
LUMINAPATH_FRONTEND_IMAGE=dominictosku/luminapath-frontend:1.2.3
```

## For developers

```powershell
$env:LUMINAPATH_API_IMAGE="dominictosku/luminapath-api:latest"
$env:LUMINAPATH_FRONTEND_IMAGE="dominictosku/luminapath-frontend:latest"
docker compose --env-file .env -f docker-compose.yml -f docker-compose.frontend.yml build
docker push dominictosku/luminapath-api:latest
docker push dominictosku/luminapath-frontend:latest
```
