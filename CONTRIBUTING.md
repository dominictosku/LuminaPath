# Contributing to LuminaPath

Bug reports, documentation improvements and pull requests are welcome. For larger
changes, open an issue first so we can agree on scope. Report vulnerabilities using
[SECURITY.md](SECURITY.md).

## Development setup

Use Git, Docker with Compose v2, the .NET 10 SDK and Node.js 22 with npm.
Follow the [local setup runbook](docs/05-runbooks/local-setup.md), including the
required values in `.env`. Never commit your `.env`, signing keys or personal data.

From the repository root:

```sh
dotnet restore LuminaPath.sln
dotnet build LuminaPath.sln --configuration Release --no-restore
dotnet test LuminaPath.sln --configuration Release --no-build
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
