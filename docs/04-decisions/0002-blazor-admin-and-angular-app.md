# ADR-0002 — Two front-ends: Blazor admin + Angular user app

- **Status**: Accepted
- **Date**: 2026-05-16
- **Deciders**: Dominic

## Context

LuminaPath ships two distinct user interfaces:

- An **Angular 21 + Ionic** app for everyday end-user use (library, quest board, planning, skill tree). Designed to run as a web SPA and as a Capacitor-wrapped native mobile app.
- A **Blazor Server** UI inside the API project for admin / debugging surfaces.

This is unusual — most projects pick one. The question came up: should we collapse to one front-end and reduce the surface?

## Decision

We will keep both.

- **Angular + Ionic** owns user-facing features. It is the only thing a normal user opens.
- **Blazor** owns admin, internal tooling, and any view that benefits from being colocated with the server (one-click ops on the DB, ad-hoc data inspection, full-trust server interactivity).

## Consequences

- **Positive**
    - User app stays mobile-first and ships native via Capacitor without dragging admin baggage along.
    - Admin features can be written in C# with direct access to services, no API design tax for one-off screens.
    - Each side can iterate on visuals independently — the user app can change its theme system without touching admin.
- **Negative**
    - Two component libraries to learn; theme tokens have to be defined in two places if we want visual parity.
    - Risk of admin features quietly drifting into "I'll just do it in Blazor because it's easier" when they should be in the user app.
- **Neutral / follow-ups**
    - We should keep a clear rule: **if an end user will ever see it, it goes in the Angular app**. Admin = operator only.

## Alternatives considered

| Option | Why we didn't pick it |
|---|---|
| Angular only | Every admin tool would need a REST endpoint; high friction for one-off ops. |
| Blazor only | Mobile story is weaker; native wrapping via .NET MAUI / Hybrid is heavier than Capacitor for the use case. |
| Razor Pages for admin | No real win over Blazor and loses interactivity. |

## Links

- [`src/LuminaPath/`](../../src/LuminaPath/) — the Blazor + API project.
- [`src/LuminaPath.MobileApp/`](../../src/LuminaPath.MobileApp/) — the Angular app.
