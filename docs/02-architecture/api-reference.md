# API reference

The .NET API ships with **Swagger / OpenAPI** out of the box. It is the source of truth for endpoint shapes — request/response DTOs are generated directly from the C# code, so it can't drift.

## Browsing the live API

Once the API is running (see [local setup](../05-runbooks/local-setup.md)):

| Surface | URL |
|---|---|
| Swagger UI (interactive) | <http://localhost:8080/swagger> |
| OpenAPI JSON | <http://localhost:8080/openapi/v1.json> |

The launch profile already opens `/swagger` when you `dotnet run` the API project locally.

## Authentication for `Try it out`

Most endpoints require an authenticated user (cookie auth via ASP.NET Core Identity). To call them from Swagger UI:

1. POST to the login endpoint with a seeded user (see [Seeded Login](https://github.com/dominictosku/LuminaPath/blob/main/README.md#seeded-login)).
2. The cookie sticks to the browser — `Try it out` will then succeed on protected routes.

## Controller surface

Each route is grouped under a controller in [`src/LuminaPath.Infrastructure/Controllers/`](../../src/LuminaPath.Infrastructure/Controllers/).

| Group | Controller | Purpose |
|---|---|---|
| Catalog | `GamesController`, `MoviesController`, `SeriesController`, `AnimesController` | Read public media metadata, list DLC / seasons. |
| Library | `MyGamesController`, `MyMoviesController`, `MySeriesController`, `MyAnimesController` | CRUD on user-owned library entries (status, rating, progress). |
| Quests | `QuestsController` | Board, skills, per-game quests, subtasks, reorder. |
| Planning | `GamingSessionsController` | Scheduled & logged sessions, forecast input. |
| Social | `FriendsController`, `DirectMessagesController` | Friend graph, DMs. |
| Browse | `BrowseController` | Cross-type search across the catalog. |
| Files | `FilesController` | Cover uploads, served from local FS or Azure Blob. |
| Integrations | `SteamController`, `ChatController` | Steam linking, AI assistant streaming. |

## Generating a typed client

If you want a typed TypeScript client for the mobile app, generate from the OpenAPI spec:

```bash
npx @openapitools/openapi-generator-cli generate \
  -i http://localhost:8080/openapi/v1.json \
  -g typescript-angular \
  -o src/LuminaPath.MobileApp/src/app/api-client
```

Currently the mobile app uses hand-written services under `src/app/features/*/services/`. Switching to a generated client is a future decision worth recording as an ADR.

## When to update this page

- **Don't** when you add a new endpoint — Swagger auto-discovers it.
- **Do** when you add a new controller *group* — add it to the table above so the catalog stays browsable.
- **Do** when authentication semantics change (new role, new auth flow).
