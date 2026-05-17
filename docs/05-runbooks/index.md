# Runbooks

Operational guides — how to run, deploy, debug, and recover LuminaPath. Written for the operator (which is currently also the developer).

## Catalog

| Runbook | When you need it |
|---|---|
| [Local setup](local-setup.md) | First time running the stack on a laptop. |
| [Database migrations](migrations.md) | Creating EF migrations and applying reviewed SQL scripts in production. |
| [Adding a media type](../backend-media-types.md) | Adding (e.g.) books or manga to the catalog. |

## Style

A good runbook:

- Starts with the **outcome** ("after this you will have…").
- Lists **prerequisites** before the first command.
- Uses **copy-pasteable commands** — no `<placeholder>` syntax inside what looks like a command.
- States what success looks like ("you should see `Now listening on http://+:8080`").
- Has a "**when it goes wrong**" section at the bottom for the failure modes you've actually hit.
