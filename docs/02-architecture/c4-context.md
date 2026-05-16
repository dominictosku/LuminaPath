# C4 Level 1 — System Context

This diagram shows LuminaPath as a single box, the people who use it, and the systems it integrates with. It is intentionally light on technology — that's the next zoom level.

```mermaid
%%{init: {'theme':'dark'}}%%
flowchart TB
    subgraph external_users["External users"]
        user(["👤 Player / Watcher<br/>Tracks media, quests, skills"])
        friend(["👤 Friend<br/>Compares progress, social feed"])
    end

    luminapath["🏠 LuminaPath<br/><i>Self-hosted media library + quest board</i>"]

    subgraph external_systems["External systems"]
        igdb["🎮 IGDB<br/>Game catalog metadata"]
        news["📰 Google News / Steam<br/>Game news feed"]
        storage["📦 Azure Blob (optional)<br/>Cover image storage"]
    end

    user -- "Browses library, tracks progress,<br/>plans sessions, completes quests" --> luminapath
    friend -- "Views shared activity" --> luminapath
    luminapath -- "Searches catalog,<br/>imports metadata" --> igdb
    luminapath -- "Pulls recent updates<br/>per game" --> news
    luminapath -- "Stores cover art<br/>(optional)" --> storage

    classDef person fill:#1e3a8a,stroke:#60a5fa,color:#fff
    classDef system fill:#0f172a,stroke:#22d3ee,color:#fff
    classDef ext fill:#1f2937,stroke:#94a3b8,color:#fff
    class user,friend person
    class luminapath system
    class igdb,news,storage ext
```

## Out of scope

- **Telemetry / analytics**: LuminaPath does not phone home. There is no third-party analytics SDK.
- **Email / push providers**: Not currently used. Achievement notifications are in-app only.
- **Payments**: Free, self-hosted, no billing path.

## Trust boundary

Everything inside the LuminaPath box is owned by the operator (the user, on their own hardware). External systems are treated as **untrusted** — responses are validated and cached server-side, and outages must degrade gracefully (the app keeps working without IGDB / news).
