# Local setup

After this you will have the API, PostgreSQL, Redis and the Angular app running on your laptop, reachable on:

- API: <http://localhost:8080>
- Angular app: <http://localhost:8100>
- Postgres: `localhost:5432` (creds in `.env`)

## Prerequisites

- Docker Desktop / Docker Engine 24+ with Compose v2
- Node.js 22+ (only if you want the Angular dev server running outside Docker)
- .NET 10 SDK (only if you want to run the API outside Docker)
- A populated `.env` file at the repo root (copy from `.env.example`)

## Option A — everything in Docker

For a local throwaway database, enable startup migrations in `.env` before the first run:

```text
RUN_MIGRATIONS_ON_STARTUP=true
```

```bash
docker compose -f docker-compose.yml -f docker-compose.frontend.yml up --build
```

Success looks like:

```
luminapath-api       | Now listening on: http://[::]:8080
luminapath-frontend  |  ➜  Local:   http://localhost:8100/
```

## Option B — backend in Docker, frontend in `ng serve`

Useful if you're iterating on the mobile app and want fast HMR.

```bash
# Terminal 1 — backend
docker compose up db redis api

# Terminal 2 — frontend
cd src/LuminaPath.MobileApp
npm install
npm run start
```

## When it goes wrong

| Symptom | Likely cause | Fix |
|---|---|---|
| `relation "X" does not exist` on API startup | Migrations didn't run | For local only, confirm `RUN_MIGRATIONS_ON_STARTUP=true` in `.env`, restart `api` |
| `connection refused` from API to `db` | Postgres still starting | API has a healthcheck; wait ~10s and it'll retry |
| Angular app shows CORS errors | Frontend origin not in `Cors__AllowedOrigins__*` | Add the origin in `.env` and restart `api` |
| Cover images don't appear | Storage volume not writable | `docker volume inspect luminapath_luminapath-storage`, check permissions |
| Port 8080 already in use | Another process is bound | `BACKEND_HTTP_PORT=8081 docker compose up` |

## Tearing it all down

```bash
docker compose down -v   # drops the volumes too — fresh DB on next start
```
