# Updating the app

After this you will have a repeatable checklist for upgrading LuminaPath dependencies, including major .NET upgrades such as .NET 10 to 11 and Angular upgrades in the mobile app.

## Current upgrade surface

LuminaPath currently has these main version anchors:

- Backend projects target `net10.0`.
- The Dockerfile uses `mcr.microsoft.com/dotnet/sdk:10.0` and `mcr.microsoft.com/dotnet/aspnet:10.0`.
- EF Core and ASP.NET package references are pinned in the `.csproj` files.
- The optional mobile app uses Angular 21, Ionic 8 and Capacitor 8.
- There is no `global.json` yet, so the local .NET SDK is selected by the machine running the command.

## Before you start

1. Create a branch for the upgrade.
2. Make sure the current main branch builds and tests pass.
3. Back up any database you use for manual verification.
4. Read the upstream release notes for the target .NET, Angular, Ionic and Capacitor versions.
5. Upgrade one major stack at a time. Prefer `.NET first, Angular second` or the reverse, not both in the same first commit.

Baseline checks from the repository root:

```bash
dotnet --info
dotnet build LuminaPath.sln
dotnet test src/LuminaPath.Tests/LuminaPath.Tests.csproj
```

Baseline checks for Angular:

```bash
cd src/LuminaPath.MobileApp
npm install
npm run build
npm run test:smoke
```

## Inventory outdated packages

Backend:

```bash
dotnet list src/LuminaPath/LuminaPath.csproj package --outdated
dotnet list src/LuminaPath.Infrastructure/LuminaPath.Infrastructure.csproj package --outdated
dotnet list src/LuminaPath.Tests/LuminaPath.Tests.csproj package --outdated
```

Frontend:

```bash
cd src/LuminaPath.MobileApp
npm outdated
npx ng update
```

The `npx ng update` command is especially useful before an Angular major upgrade because it reports the supported migration path for the installed Angular CLI.

## Upgrade .NET

Use this section for upgrades such as .NET 10 to .NET 11.

1. Install the target .NET SDK locally and on CI/build agents.
2. Optional but recommended: add a `global.json` so everyone builds with the same SDK feature band.
3. Update all backend `TargetFramework` values.
4. Update Microsoft package references to the target major version.
5. Update the Docker SDK and runtime images.
6. Update documentation badges and tech-stack references.
7. Build, test and run the app.

Find the files that mention the old version:

```bash
rg "net10.0|dotnet/sdk:10.0|dotnet/aspnet:10.0|\\.NET 10|\\.NET-10"
```

Typical files to update:

- `src/LuminaPath/LuminaPath.csproj`
- `src/LuminaPath.Core/LuminaPath.Core.csproj`
- `src/LuminaPath.Infrastructure/LuminaPath.Infrastructure.csproj`
- `src/LuminaPath.Tests/LuminaPath.Tests.csproj`
- `Dockerfile`
- `README.md`
- docs that mention the runtime version

Example version changes for a .NET 10 to 11 upgrade:

```xml
<TargetFramework>net11.0</TargetFramework>
```

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
FROM mcr.microsoft.com/dotnet/aspnet:11.0 AS runtime
```

After editing, restore and verify:

```bash
dotnet restore LuminaPath.sln
dotnet build LuminaPath.sln
dotnet test src/LuminaPath.Tests/LuminaPath.Tests.csproj
```

If EF Core changed major versions, create a temporary migration only when a model change is expected. If the model did not change, there should be no new migration. Production schema changes must still go through the [database migrations runbook](migrations.md).

## Upgrade Angular, Ionic and Capacitor

The Angular app lives in `src/LuminaPath.MobileApp` and uses `package-lock.json`, so use `npm` for dependency changes.

Start with Angular's own migrations:

```bash
cd src/LuminaPath.MobileApp
npx ng update @angular/core@22 @angular/cli@22
```

The command above is the current next-major example from Angular 21 to Angular 22. For a later upgrade, replace the target major version before running it.

Then update related Angular packages together:

- `@angular/*`
- `@angular-devkit/build-angular`
- `@angular-eslint/*`
- `typescript`, only within Angular's supported range
- `zone.js`, only within Angular's supported range

Update Ionic and Capacitor separately after Angular builds again:

```bash
npm install @ionic/angular@latest @ionic/angular-toolkit@latest
npm install @capacitor/core@latest @capacitor/cli@latest @capacitor/android@latest
npx cap sync
```

After every major frontend upgrade:

```bash
npm run lint
npm run build
npm run test:smoke
```

If `npm run lint` fails because the lint configuration changed, fix the configuration instead of skipping lint permanently.

## Check integration points

After either backend or frontend upgrades, manually verify:

- Login and logout.
- Library list, filters, infinite loading and details.
- Game details tabs, trophies, quests and notes.
- Planning calendar.
- Settings tabs, database backups and background jobs.
- Import/export flows for ODS and PSN.
- Uploaded cover images and documents.

For Docker:

```bash
docker compose build
docker compose up -d
docker compose ps
```

Open the backend and optional Angular frontend and check that API calls still work through the configured proxy/base URL.

## Production rollout

1. Build the release image.
2. Generate and review a production migration script if EF model changes exist.
3. Back up PostgreSQL before deploying.
4. Apply the reviewed migration script manually.
5. Deploy the new image with `RUN_MIGRATIONS_ON_STARTUP=false`.
6. Run a smoke test in the browser.
7. Check background job history and application logs.

## Rollback

Keep the previous Docker image tag available until the new version has been verified.

If a deployment fails before schema changes were applied, roll back the app image.

If a deployment fails after schema changes were applied, prefer a forward fix. Only restore the database backup when the migration caused data loss or the app cannot safely run against the migrated schema.

## When it goes wrong

| Symptom | Likely cause | Fix |
|---|---|---|
| `NETSDK1045` or unsupported target framework | The installed SDK is older than the target framework | Install the target SDK and, if needed, add/update `global.json`. |
| Docker build still uses the old runtime | `Dockerfile` was not updated | Update both SDK and ASP.NET runtime image tags. |
| EF tooling fails after a .NET upgrade | `dotnet-ef` or EF packages are on the old major version | Update `dotnet-ef` and align EF package versions. |
| Angular update refuses to run | The current workspace version is too far behind or peer deps conflict | Run `npx ng update` without package names and follow the recommended intermediate version. |
| TypeScript errors after Angular update | TypeScript version is outside Angular's supported range | Install the TypeScript version range recommended by Angular. |
| Mobile plugins compile but native behavior breaks | Capacitor packages were updated without syncing native projects | Run `npx cap sync` and test native/mobile-specific flows. |
