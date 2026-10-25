# Contributing to LuminaPath

Bug reports, documentation improvements and pull requests are welcome. For larger
changes, open an issue first so we can agree on scope. Report vulnerabilities using
[SECURITY.md](SECURITY.md).

## Development setup

Use Git, Docker with Compose v2.30+, the .NET 10 SDK and Node.js 22 with npm.
Copy `.env.example` to `.env` at the repository root and set its three required
values as described in the [README](README.md#quick-docker-setup). Never commit
`.env`, signing keys or personal data.

### Full stack from source

```sh
docker compose -f compose.yml -f compose.dev.yml up -d --build
```

The development overlay builds local images and publishes API, PostgreSQL and
Redis on `127.0.0.1:8080`, `127.0.0.1:5432` and `127.0.0.1:6379`. The frontend
is at <http://localhost:4200>. Containers keep production application settings;
startup migrations are enabled, just as in the published-image stack.
Use the same `-f` arguments for subsequent `logs`, `stop` and `down` commands.
`docker compose ... down` keeps named volumes; adding `-v` deletes their data.

### Backend and frontend on the host

Start just the infrastructure with host ports:

```sh
docker compose -f compose.yml -f compose.dev.yml up -d db redis
```

Host processes do not load Compose's `.env`. Set the database connection for
`dotnet run` using environment variables or .NET user secrets, matching the
password in `.env` (the development JSON contains only throwaway defaults):

```sh
export ConnectionStrings__Default='Host=localhost;Port=5432;Database=luminapath;Username=luminapath;Password=your-database-password'
dotnet dev-certs https --trust
dotnet run --project src/LuminaPath/LuminaPath.csproj
```

On PowerShell use `$env:ConnectionStrings__Default="..."` instead of `export`.
The development launch profile uses <https://localhost:7013>. Development
applies migrations automatically and permits the bundled local admin account
(`admin@example.com` / `ChangeMe!1AdminAccess`). Set `Admin__Email` and
`Admin__Password` in the host environment to use your own credentials.

For frontend hot reload, set `apiEndpoint` in
`src/LuminaPath.MobileApp/src/assets/env.js` to `https://localhost:7013/api`,
then run the frontend commands below. When using the Docker API instead,
start `db redis api` with the development overlay, use `http://localhost:8080/api`,
and add `Cors__AllowedOrigins__0=http://localhost:4200` to `.env`.
Use trusted HTTPS for cross-site cookie authentication.

### Validation

From the repository root:

```sh
dotnet restore LuminaPath.sln
dotnet build LuminaPath.sln --configuration Release --no-restore
dotnet test LuminaPath.sln --configuration Release --no-build
python3 scripts/check-compose.py
```

For the frontend:

```sh
cd src/LuminaPath.MobileApp
npm ci
npm run lint
npm run test:smoke
npm run build -- --configuration production
```

The smoke tests need Chrome or Chromium. Set `CHROME_BIN` if it is not detected.
Use `npm start` for the development server at <http://localhost:4200>.
Use the npm commands so the license assets are prepared before a build.

## Manual Android APK build

Use this when you want to build the Ionic/Angular mobile app yourself and install it on a phone for homelab testing.

Install these tools first:

- Android Studio with the Android SDK.
- Java 21.
- Android platform tools if you want to install through `adb`.
- Node.js 22.

Build the web app and sync Capacitor:

```powershell
cd src/LuminaPath.MobileApp
npm install
npm run build -- --configuration production
npx cap add android
npx cap sync android
```

`npx cap add android` only needs to be run once, or whenever the generated `android` folder does not exist.
The Angular production build writes the web app to `www/browser`, which is the directory Capacitor packages into the APK.

Build a debug APK:

```powershell
cd android
.\gradlew.bat assembleDebug
```

The APK is created here:

```text
src/LuminaPath.MobileApp/android/app/build/outputs/apk/debug/app-debug.apk
```

Install it on a connected Android device:

```powershell
adb install -r app\build\outputs\apk\debug\app-debug.apk
```

For homelab testing, point the app login screen to your hosted backend API:

```text
https://luminapath.yourdomain.com/api
```

or:

```text
http://192.168.178.50:8080/api
```

HTTPS with a trusted certificate is recommended. Plain HTTP and self-signed certificates can work for some setups, but Android WebView is much stricter than a normal desktop browser.

Allow the Capacitor app origin in the backend CORS configuration:

```text
Cors__AllowedOrigins__0=capacitor://localhost
Cors__AllowedOrigins__1=http://localhost
Cors__AllowedOrigins__2=https://luminapath.yourdomain.com
Auth__CookieSameSite=None
Auth__CookieSecurePolicy=Always
```

Restart the backend after changing CORS settings.

For quick rebuilds after frontend changes:

```powershell
cd src/LuminaPath.MobileApp
npm run build -- --configuration production
npx cap sync android
cd android
.\gradlew.bat assembleDebug
adb install -r app\build\outputs\apk\debug\app-debug.apk
```

## Pull requests

- Branch from `main` and keep each pull request focused on one change.
- Explain the problem, the resulting behavior and how you verified it.
- Add meaningful tests for changed behavior and update affected documentation.
- Include migration instructions when changing the database or configuration.
- For dependencies or bundled assets, retain upstream notices and update
  [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) and `licenses/` as needed.
- Run the checks above. Documentation changes also require
  `python -m pip install -r requirements-docs.txt` and `mkdocs build --strict`.

The [documentation guide](docs/contributing.md) covers documentation conventions.
The [release runbook](docs/05-runbooks/releases.md) describes distribution artifacts.

The frontend's scoped `overrides` keep Compodoc 1.x on patched dependencies within
their existing major versions. Review those overrides when upgrading Compodoc;
remove them once its own dependency declarations include the fixes. Check both
`npm audit` and `npm audit --omit=dev` when changing dependencies.

## License

By submitting a contribution, you agree to provide it under the project's
[AGPL-3.0-only license](LICENSE). You retain copyright in your contributions.
Only submit material you have the right to license, and identify any third-party
material and its original license.
