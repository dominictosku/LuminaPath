# Architecture Decision Records

Each ADR captures *one* decision: what we picked, why, and what we gave up. They are numbered, dated, and **immutable once Accepted** — if the decision later changes, write a new ADR that supersedes the old one.

## Why ADRs

Code answers "what". Tests answer "does it work". ADRs answer **"why didn't you do it the other way"** — the question every new contributor asks within their first week.

## Index

| # | Title | Status |
|---|---|---|
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions | Accepted |
| [0002](0002-blazor-admin-and-angular-app.md) | Two front-ends: Blazor admin + Angular user app | Accepted |

## Writing a new ADR

1. Copy [`template.md`](template.md) → `NNNN-kebab-title.md` (next free number).
2. Fill in **Context**, **Decision**, **Consequences**. Keep it on one screen.
3. Open a PR with status `Proposed`. Move to `Accepted` on merge.
4. Add a row to the table above.
