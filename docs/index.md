# LuminaPath documentation

LuminaPath is a self-hosted library tracker for games, movies, series and anime, with a quest board, planning tools, a skill tree and social features.

This site is the **single source of truth for everything that isn't the code itself** — architecture, decisions, how to run things, and how each feature is supposed to behave. It is written as plain Markdown that lives next to the source so it gets reviewed, versioned and corrected like any other change.

## Where to go

| You want to… | Start at |
|---|---|
| Understand the product | [Overview](01-overview/index.md) |
| Understand how the system fits together | [Architecture](02-architecture/index.md) |
| Learn how a specific feature works | [Features](03-features/index.md) |
| Know *why* something was built a certain way | [Decisions (ADRs)](04-decisions/index.md) |
| Run, deploy or debug LuminaPath locally | [Runbooks](05-runbooks/index.md) |

## How to contribute to the docs

Documentation is part of the same review cycle as code:

1. Edit Markdown under `docs/` in the same PR as the behaviour change.
2. New architectural decisions get an [ADR](04-decisions/index.md) — short, dated, and immutable once merged.
3. New diagrams should be **Mermaid** or **PlantUML** fenced code blocks (so they version-control as text and render natively on GitHub and in this site).
4. Prefer linking over duplicating. If something is in the README, link to it; don't restate it here.

See the [docs style guide](contributing.md) for the conventions used across this site.
