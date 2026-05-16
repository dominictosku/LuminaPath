# ADR-0001 — Record architecture decisions in the repo

- **Status**: Accepted
- **Date**: 2026-05-16
- **Deciders**: Dominic

## Context

The project has been iterating on multiple non-trivial architectural choices — splitting media types into per-type entities, running both a Blazor admin and an Angular user app, theming via global CSS tokens, the MyXService pattern, etc. Those decisions are currently only visible by reading the diff and inferring intent. Past attempts at capturing architecture in Word documents went stale within months and weren't read by anyone other than the author.

We need a low-ceremony way to record *why* something is built a given way, that:

- Lives next to the code so it survives renames and reorganisations,
- Gets reviewed in the same PR as the change it describes,
- Is short enough that writing one is not a burden.

## Decision

We will use [**Architecture Decision Records (ADRs)**](https://adr.github.io/) as the canonical place for "why" decisions, stored as Markdown under [`docs/04-decisions/`](./index.md).

ADRs are numbered, dated, and **immutable once Accepted** — superseding decisions get a new ADR, not an edit.

## Consequences

- **Positive**
    - New contributors can read `04-decisions/` and understand the "why" without bothering the author.
    - History is preserved — we can see what we *used* to think and what changed our mind.
    - PRs that change architecture without an ADR become a visible smell in review.
- **Negative**
    - One more thing to remember in a PR. We mitigate by referencing ADRs from the [docs style guide](../contributing.md).
- **Neutral / follow-ups**
    - We do **not** require an ADR for every change — only ones where the future reader will ask "why didn't you just X".

## Alternatives considered

| Option | Why we didn't pick it |
|---|---|
| Word documents in a shared drive | Tried this before — go stale, nobody opens them, no PR review loop. |
| Wiki (GitHub Wiki / Confluence) | Detaches docs from code; PRs to code can merge without touching the wiki. |
| Long-form `ARCHITECTURE.md` | Works for *current* state but not for capturing *history of decisions*. We can have both. |

## Links

- [adr.github.io](https://adr.github.io/) — the original ADR proposal by Michael Nygard.
- [docs/contributing.md](../contributing.md) — ADR conventions used here.
