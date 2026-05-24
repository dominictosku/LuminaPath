# LuminaPath

Self-hosted media library + quest board for tracking games, anime, movies,
and series. .NET 10 backend (Blazor admin + REST API), Angular 21 / Ionic 8
mobile app.

## Configuration

The backend reads its settings from (in order of precedence, highest first):

1. Environment variables
2. `appsettings.{Environment}.json` (e.g. `appsettings.Production.json`)
3. `appsettings.json`

Nested config keys use `:` in JSON and **`__` (double underscore)** in env
vars — e.g. `Auth:RequireAdminApproval` → `Auth__RequireAdminApproval`.

The Docker Compose stack reads the `.env` file at the repo root (start
from `.env.example`) and maps friendly SCREAMING_SNAKE_CASE names onto
the `__`-style env vars the .NET host expects. See `docker-compose.yml`
for the mapping table.

### Seed admin credentials

On first boot — when no admin user exists yet — the backend creates one
automatically so you can sign in to the Blazor admin and the mobile app.
**Override the bundled defaults before any deployment you actually use.**
A startup warning is written to the log whenever the bundled fallback
credentials are in play.

| Setting             | Env var (raw)              | Env var (.env)             | Default                  |
| ------------------- | -------------------------- | -------------------------- | ------------------------ |
| Admin email         | `Admin__Email`             | `LUMINAPATH_ADMIN_EMAIL`   | `admin@example.com`      |
| Admin password      | `Admin__Password`          | `LUMINAPATH_ADMIN_PASSWORD`| `ChangeMe!1AdminAccess`  |

The password has to satisfy the production policy: ≥ 10 chars, ≥ 4
unique chars, uppercase + lowercase + digit + non-alphanumeric. Custom
passwords that don't satisfy this will cause the seeder to throw on
startup.

**Option A — `appsettings.json` / `appsettings.Production.json`:**

```jsonc
{
  "Admin": {
    "Email": "you@example.com",
    "Password": "your-strong-password-here"
  }
}
```

**Option B — `.env` file (Docker Compose):**

```bash
LUMINAPATH_ADMIN_EMAIL=you@example.com
LUMINAPATH_ADMIN_PASSWORD=your-strong-password-here
```

The seeder runs only when no admin user exists, so changing these
*after* the first boot does **not** rotate the admin password. To change
an existing admin's password, log in to the Blazor app and use
`/Account/Manage/ChangePassword`, or reset it from the Users page.

### Require admin approval for new registrations

When this gate is on, newly self-registered users land as
`IsActive=false` and cannot sign in until an admin flips the flag on
the Blazor `/Admin/Users` page.

| Setting                | Env var (raw)                | Env var (.env)                | Default (Production)        | Default (Development) |
| ---------------------- | ---------------------------- | ----------------------------- | --------------------------- | --------------------- |
| Require admin approval | `Auth__RequireAdminApproval` | `AUTH_REQUIRE_ADMIN_APPROVAL` | **`true`** (approval on)    | `false` (immediate)   |

The defaults are wired through `appsettings.json` (sets `true`) and
`appsettings.Development.json` (overrides to `false`), so the
production-safe behavior is on without any explicit configuration, and
local dev with `ASPNETCORE_ENVIRONMENT=Development` doesn't make you
click "Activate" after every test registration.

**To force it on locally** (e.g. to reproduce the production approval
flow) — set in `appsettings.Development.json`:

```jsonc
{
  "Auth": {
    "RequireAdminApproval": true
  }
}
```

**To allow immediate sign-in in production** (for example, a private
deployment where every visitor is trusted) — set in `appsettings.Production.json`
or via env:

```jsonc
{
  "Auth": {
    "RequireAdminApproval": false
  }
}
```

```bash
# Docker Compose .env
AUTH_REQUIRE_ADMIN_APPROVAL=false
```

How it behaves end-to-end:

- **Self-registration via mobile app or Blazor `/Account/Register`:**
  user is created with `IsActive=false`. The mobile app's login screen
  will report a generic "Sign-in failed" until activation; the Blazor
  page redirects to a "pending administrator approval" confirmation
  message.
- **Admin-created users via the Blazor Users page:** the admin's
  `Active` checkbox in the create form wins. The approval gate only
  applies to anonymous self-registration.
- **The seeded admin and any pre-existing user:** unaffected. Existing
  rows are backfilled to `IsActive=true` on migration so nobody gets
  locked out on upgrade.
- **Activating a user:** open the Blazor admin at `/Admin/Users`, find
  the row, click the kebab menu, choose **Activate**. The user can sign
  in immediately afterwards. (Activating also clears any auto-lockout
  from failed sign-in attempts in the same click.)

### Quick first-boot checklist

1. Copy `.env.example` → `.env` and set:
   - `POSTGRES_PASSWORD` (database)
   - `LUMINAPATH_ADMIN_EMAIL` + `LUMINAPATH_ADMIN_PASSWORD` (admin seed)
   - Leave `AUTH_REQUIRE_ADMIN_APPROVAL=true` (the production default) —
     or set it to `false` if every visitor is trusted in your deployment
2. `docker compose up -d`
3. Sign in to the Blazor admin at `https://<your-host>/Account/Login`
   using the credentials from step 1.
4. (Approval gate verification) Register a new account in the mobile
   app, then verify it cannot sign in until you click **Activate** on
   the row in `/Admin/Users`.
