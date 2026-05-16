# Architecture

LuminaPath is documented using the [C4 model](https://c4model.com/) — four nested levels of zoom:

| Level | Question it answers | Page |
|---|---|---|
| **1. System Context** | Who uses LuminaPath and what does it talk to? | [Context diagram](c4-context.md) |
| **2. Containers** | What deployable units make up the system? | [Container diagram](c4-container.md) |
| **3. Components** | What's inside each container? | Per-feature pages under [Features](../03-features/index.md) |
| **4. Code** | How are individual classes/files structured? | The code itself + IDE; we don't draw this level. |

The why's behind each split live in the [Architecture Decision Records](../04-decisions/index.md).

## Tech inventory

| Layer | Technology |
|---|---|
| Backend API | .NET 10, ASP.NET Core, EF Core |
| Backend UI (admin) | Blazor Server |
| Mobile / web app | Angular 21 + Ionic |
| Database | PostgreSQL 17 |
| Cache / sessions | Redis |
| Storage | Local filesystem or Azure Blob (pluggable) |
| Reverse proxy | nginx |
| Container runtime | Docker Compose |
| External catalog sources | IGDB (games), Google News / Steam (game news) |

## Repository layout

```
LuminaPath/
├── src/
│   ├── LuminaPath/                # ASP.NET Core API + Blazor admin
│   ├── LuminaPath.Core/           # Domain models, DTOs, mappings
│   ├── LuminaPath.Infrastructure/ # EF Core, repositories, integrations
│   ├── LuminaPath.MobileApp/      # Angular + Ionic frontend
│   ├── LuminaPath.UI.Shared/      # Shared Razor components
│   └── LuminaPath.Tests/          # Backend unit + integration tests
├── docker-compose.yml             # API + DB + Redis (production-shape)
├── docker-compose.frontend.yml    # Adds the Angular dev server
└── docs/                          # You are here.
```
