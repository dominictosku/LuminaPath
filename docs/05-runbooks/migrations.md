# Database migrations

After this you will know how LuminaPath applies EF Core migrations in development, and how to generate a reviewed SQL script for production.

## Current backend behavior

- In `Development`, the backend calls `MigrateDatabase()` during startup and applies pending EF Core migrations automatically.
- Outside `Development`, migrations are only applied on startup when `Database:RunMigrationsOnStartup` is `true`.
- The default production configuration is `false`. Keep it that way for real deployments.
- Docker Compose maps `RUN_MIGRATIONS_ON_STARTUP` to `Database__RunMigrationsOnStartup`. The compose default is `false`.

## Local development

If you run the Blazor backend with `ASPNETCORE_ENVIRONMENT=Development`, migrations are applied automatically on startup.

If you run the backend in Docker with `ASPNETCORE_ENVIRONMENT=Production` against a local throwaway database, you can opt into automatic migrations in `.env`:

```text
RUN_MIGRATIONS_ON_STARTUP=true
```

Use this only for local/dev databases where it is acceptable for the app process to change the schema at startup.

## Create a migration

Install or update the EF CLI if needed:

```bash
dotnet tool install --global dotnet-ef
```

If it is already installed, update it instead:

```bash
dotnet tool update --global dotnet-ef
```

Create the migration from the repository root:

```bash
dotnet ef migrations add BackgroundJobs --project src/LuminaPath.Infrastructure/LuminaPath.Infrastructure.csproj --startup-project src/LuminaPath/LuminaPath.csproj --output-dir Migrations
```

Use a descriptive migration name. Review the generated files under `src/LuminaPath.Infrastructure/Migrations/`.

## Generate a production SQL script

Generate an idempotent script from the repository root:

```bash
mkdir -p artifacts/migrations
dotnet ef migrations script --idempotent --project src/LuminaPath.Infrastructure/LuminaPath.Infrastructure.csproj --startup-project src/LuminaPath/LuminaPath.csproj --output artifacts/migrations/luminapath-migration.sql
```

Review the generated SQL before applying it. Commit the migration `.cs` files, not the generated environment-specific SQL artifact.

## Apply in production

1. Back up the production database.
2. Put the app in a deployment window or maintenance mode if the migration changes hot paths.
3. Apply the reviewed SQL script with a database operator account.
4. Start the new application version with startup migrations disabled.

Example with `psql`:

```bash
psql "host=db port=5432 dbname=luminapath user=luminapath" --file artifacts/migrations/luminapath-migration.sql
```

Production environment:

```text
RUN_MIGRATIONS_ON_STARTUP=false
```

## When it goes wrong

| Symptom | Likely cause | Fix |
|---|---|---|
| App starts locally with `relation "X" does not exist` | Local Docker backend is running as `Production` with startup migrations disabled | Set `RUN_MIGRATIONS_ON_STARTUP=true` for local only, or run the backend as `Development`. |
| Production app applies migrations on startup | `RUN_MIGRATIONS_ON_STARTUP=true` was deployed | Set it to `false` and apply reviewed SQL scripts manually. |
| `dotnet ef` cannot create the app service provider | Startup config requires services/secrets not available locally | Use local appsettings/user-secrets/env vars for required config, then regenerate the script. |
| SQL script fails in production | Database state differs from expected migrations history | Check `__EFMigrationsHistory`, restore from backup if needed, and regenerate an idempotent script from the deployed code version. |
