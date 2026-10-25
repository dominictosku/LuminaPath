# Local setup

The canonical development instructions are in
[CONTRIBUTING.md](https://github.com/dominictosku/LuminaPath/blob/main/CONTRIBUTING.md#development-setup).
They cover local image builds, running backend/frontend on the host, validation
and Android builds.

For published-image installation, updates and environment variables, use the
[README](https://github.com/dominictosku/LuminaPath/blob/main/README.md#quick-docker-setup).

## Troubleshooting

| Symptom | Check |
|---|---|
| Missing database tables | Startup migrations default to on in Docker. If disabled, apply the reviewed schema first. |
| API cannot reach Redis | Use `Redis__ConnectionString=redis:6379` in Docker; do not use a host-published port internally. |
| Host process cannot reach Postgres | Start `db` with `compose.dev.yml`; set the host connection string to match `.env`. |
| Browser CORS errors during hot reload | Match the frontend API URL and the backend's allowed origin; see the contributor setup. |
| Missing uploaded files after migration | Keep the original Compose project name and storage volume, and check mount paths. |

Use `docker compose logs api` for startup diagnostics. Named volumes survive
`docker compose down`; `down -v` deletes their data.
