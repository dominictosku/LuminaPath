# Configuration reference

The canonical [environment variable reference](https://github.com/dominictosku/LuminaPath/blob/main/README.md#environment-variables)
is in the repository README, alongside installation, updates, secrets and
migration instructions. Only the three values in `.env.example` are needed for
the default Docker stack; add optional overrides only when needed.

Application defaults live in the backend configuration and option classes.
Compose supplies container wiring and passes `.env` to the API; optional backend
keys use the native .NET `Section__Property` convention.

For development setup and host processes, see
[CONTRIBUTING.md](https://github.com/dominictosku/LuminaPath/blob/main/CONTRIBUTING.md#development-setup).
For deliberate schema changes, see [database migrations](migrations.md).
