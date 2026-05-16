# Overview

LuminaPath is a self-hosted "command center" for someone who wants to keep their games, movies, series and anime in one place — track what's in the backlog, what's currently being played/watched, how much time was spent, what is planned for this week, what friends are doing, and how that ladders up into long-term real-life skill goals.

## Audience

- **Player / watcher**: wants to track progress and avoid backlog amnesia.
- **Planner**: wants forecasting ("at the current pace I finish *Elden Ring* on May 28th").
- **Self-improver**: wants to convert media time and real-life practice into a skill tree.

## Target deployment

A single home server (NUC, mini-PC, Raspberry Pi 5, NAS) running Docker. No SaaS dependency, no telemetry, full data ownership.

See the [README](https://github.com/dominictosku/LuminaPath/blob/main/README.md) for the marketing-level pitch and feature list. This site is for everything that doesn't fit there.

## Glossary

| Term | Meaning |
|---|---|
| **Media** | Catalog item: a game, movie, series episode, or anime (the public metadata). |
| **MyMedia** | A user-owned library entry pointing at a Media. Holds status, rating, progress, dates, notes. |
| **Quest** | A real-world task. Can be life-only, linked to a game, or linked to a skill node. |
| **Skill** | A user-defined real-life ability (Programming, Drawing…) that earns XP and unlocks nodes. |
| **Skill node** | A milestone on a skill ("first portrait", "30-day streak"). |
| **Forecast** | Projected completion date for a media item, based on logged sessions + scheduled sessions. |
| **Library** | The collection of MyMedia entries belonging to one user. |
