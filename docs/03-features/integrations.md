# Third-Party Integrations

LuminaPath integrates with external gaming platforms, cloud calendars, and metadata catalogs to enrich your media collection and keep your release dates synchronized.

---

## Google Calendar Integration

The Google Calendar integration pushes upcoming releases from your library and scheduled quests directly into a dedicated Google Calendar.

### Key Details

- **One-Way Sync**: LuminaPath pushes events to Google Calendar (it does not modify your personal calendars).
- **Dedicated Calendar**: Events are written into an isolated calendar (defaults to `LuminaPath`), keeping your personal schedule clean.
- **Deduplication**: Events use deterministic IDs derived from the game/quest ID, preventing duplicates upon re-syncing.
- **Encrypted Storage**: OAuth refresh tokens are encrypted at rest using ASP.NET Core Data Protection (`App_Data/keys`).

### Setup & Authorization

1. In the [Google Cloud Console](https://console.cloud.google.com/), enable the **Google Calendar API**.
2. Create an **OAuth 2.0 Web Client**:
   - Authorized Redirect URI: `https://<your-frontend-host>/api/integrations/google/callback`
3. Configure the credentials in one of two ways:
   - **Via Admin UI (Recommended)**: Sign in to the Blazor Admin interface, open **Settings → Google Calendar**, enter your Client ID, Client Secret, and desired Calendar Name, and save. This takes effect immediately without restarting the container.
   - **Via `.env`**:
     ```dotenv
     GOOGLE_CLIENT_ID=your-client-id.apps.googleusercontent.com
     GOOGLE_CLIENT_SECRET=your-client-secret
     GoogleCalendar__CalendarName=LuminaPath
     ```
4. Once configured, users can navigate to **Settings → Google Calendar** in the mobile/web app, link their Google account, and click **Sync now**.

---

## Gaming Platform Imports

### Steam

LuminaPath can import your owned games, recorded playtime, and cover art directly from your Steam profile.

- **Configuration**:
  ```dotenv
  Steam__ApiBaseUrl=https://api.steampowered.com
  Steam__StoreBaseUrl=https://store.steampowered.com
  ```
- **Usage**: Enter your Steam ID or profile URL in the import screen. Games will be matched against the catalog, and your play hours will be recorded as verified third-party playtime.

### PlayStation Network (PSN)

Import your PlayStation games, playtime, and trophies:

- Configured using standard Sony PSN endpoints.
- Allows tracking PSN playtime alongside manual entries.

---

## Game Metadata Catalogs

LuminaPath automatically enriches games with release dates, cover artwork, genres, and developer information using IGDB (Internet Game Database) and RAWG.

### Configuration

```dotenv
GameMetadata__Provider=IgdbThenRawg
GameMetadata__IgdbClientId=your-twitch-client-id
GameMetadata__IgdbClientSecret=your-twitch-client-secret
GameMetadata__RawgApiKey=your-rawg-api-key
```

### Provider Resolution Order

The `GameMetadata__Provider` setting controls which provider is queried first:

- `IgdbThenRawg` (default): Tries IGDB first; if no match or rate-limited, falls back to RAWG.
- `RawgThenIgdb`: Tries RAWG first; falls back to IGDB.
- `Igdb`: Only queries IGDB.
- `Rawg`: Only queries RAWG.
