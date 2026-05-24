# LuminaPath Docker Hub Deployment

This folder is for users who want to run LuminaPath from prebuilt Docker Hub images instead of building the repository locally.

## Start

Copy the example environment file:

```powershell
Copy-Item .env.example .env
```

Edit `.env` and set at least:

```text
POSTGRES_PASSWORD=your-strong-password
FRONTEND_PUBLIC_URL=https://luminapath.yourdomain.com
LUMINAPATH_ADMIN_EMAIL=you@example.com
LUMINAPATH_ADMIN_PASSWORD=your-strong-admin-password
```

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

If you put LuminaPath behind a reverse proxy, set the public frontend URL:

```text
FRONTEND_PUBLIC_URL=https://luminapath.yourdomain.com
AUTH_COOKIE_SAMESITE=Lax
AUTH_COOKIE_SECURE_POLICY=Always
```

Keep this value when using the included frontend container:

```text
LUMINAPATH_API_ENDPOINT=/api
LUMINAPATH_API_PROXY_TARGET=http://luminapath-api:8080
```

The frontend container proxies `/api` to the backend container internally.

Only add additional CORS origins when you intentionally serve the frontend
from a different origin than the API. If you do that, set exact HTTPS origins
and use `AUTH_COOKIE_SAMESITE=None` only for those cross-site cookie flows.

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

$env:LUMINAPATH_API_IMAGE="sekijuo/luminapath-api:latest" 
$env:LUMINAPATH_FRONTEND_IMAGE="sekijuo/luminapath-frontend:latest"
docker compose --env-file .env -f docker-compose.yml -f docker-compose.frontend.yml build
docker push sekijuo/luminapath-api:latest  
docker push sekijuo/luminapath-frontend:latest
