# Docs style guide

## Folder layout

```
docs/
├── index.md                  # site entry
├── 01-overview/              # what LuminaPath is, who it is for, screenshots
├── 02-architecture/          # how the system fits together (C4, deployment)
├── 03-features/              # one folder per user-facing feature
├── 04-decisions/             # ADRs (numbered, dated, immutable)
└── 05-runbooks/              # local setup, deploy, debug, ops
```

## Conventions

- **One H1 per page**, matching the page title in `mkdocs.yml`.
- **Sentence case** for headings ("Adding a media type", not "Adding A Media Type").
- **Mermaid for diagrams** (`mermaid` fenced code block). Use PlantUML only when Mermaid can't express it.
- **Relative links** between docs (`../04-decisions/0001-record-architecture-decisions.md`), not absolute URLs.
- **Code blocks must specify a language** (`csharp`, `typescript`, `bash`, etc.) so syntax highlighting works.
- **Never paste secrets** — point at `.env.example` instead.
- **Don't restate the README** — link to it.

## When to write what

| Change | What to update |
|---|---|
| New feature | A page under `03-features/` + a sequence diagram for the happy path |
| Refactor / new pattern | An ADR under `04-decisions/` |
| New service, container or external dependency | The C4 Container diagram in `02-architecture/` |
| New environment variable or deploy step | A runbook under `05-runbooks/` |
| Bug fix | Usually nothing — the test + commit message is the doc |

## ADR rules

- Numbered `NNNN-kebab-case-title.md`.
- Status is one of: `Proposed`, `Accepted`, `Deprecated`, `Superseded by ADR-XXXX`.
- **Once `Accepted` and merged, the body is immutable.** Don't edit history; supersede with a new ADR.
- Keep it under one screen. The point is to capture the *why* fast, not to write a whitepaper.
