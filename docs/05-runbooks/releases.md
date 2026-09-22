# Publishing releases

A version tag produces Docker Hub images, an Android APK and a GitHub release
containing the exact source snapshot, license notices and SHA-256 checksums.
GHCR and Azure publishing is a separate, manual workflow.

## Before the first public release

1. Include the AGPL license, notices and publication changes in `main`.
2. Set the GitHub default branch to the intended public branch and make the
   source repository public. Review existing Actions logs and artifacts before
   changing a previously private repository's visibility.
3. Enable private vulnerability reporting under the repository's security
   settings so the link in `SECURITY.md` is available.
4. Add `DOCKERHUB_USERNAME` and `DOCKERHUB_TOKEN` as repository Actions secrets.
   Ensure the two Docker Hub repositories are available for public pulls.
5. Require a passing CI and Docs run on the commit being released.

Android releases use a debug signing key unless all four signing secrets are set:
`ANDROID_KEYSTORE_BASE64`, `ANDROID_KEYSTORE_PASSWORD`, `ANDROID_KEY_ALIAS` and
`ANDROID_KEY_PASSWORD`. Keep the same release key for future app updates.

## Refreshing dependency notices

Use a .NET Release build to refresh the resolved package manifest, then run:

```sh
dotnet build src/LuminaPath/LuminaPath.csproj --configuration Release
python scripts/update-backend-licenses.py
```

The script reads the restored NuGet cache and downloads missing license texts
from the package's pinned upstream commit. Review the generated files under
`licenses/`, particularly any new license or upstream NOTICE.

For native Android dependencies, run from `src/LuminaPath.MobileApp` after `npm ci`:

```sh
npm run build -- --configuration production
npx cap sync android
cd android
./gradlew :app:dependencies --configuration releaseRuntimeClasspath --console=plain > ../../../artifacts/android-dependencies.txt
cd ../../..
python scripts/update-android-licenses.py artifacts/android-dependencies.txt
```

Create `artifacts/` first if it does not exist. On Windows, use `gradlew.bat`.
The native notice script accepts only reviewed Apache-2.0 Maven families and
copies Capacitor notices from the installed npm packages. Build an APK to cache
native archives before regenerating notices when dependencies change, so any
embedded NOTICE files can also be retained. Rebuild the web app and resync
Capacitor after updating notices to include the new texts in the APK.

`python scripts/update-bundled-licenses.py` refreshes the explicitly pinned
Bootstrap, Tailwind and Apache license files. Update its version references if
replacing those vendored assets. Use `npm run build` for frontend releases;
its postbuild step includes Angular's extracted notices in the served assets.

## Publishing a version

After merging and verifying the intended commit:

```sh
git tag v1.2.3
git push origin v1.2.3
```

The `Release` workflow resolves the tag to a commit and uses that same commit for
both images, the APK and the source archive. Manual runs require an existing tag;
they do not create a tag from an unrelated branch. The GitHub release is published
only after both image and APK jobs succeed. Source and notice files remain in
workflow artifacts if a later job fails and needs to be rerun.

Release assets include `LuminaPath-<version>-source.tar.gz`,
`LuminaPath-<version>-licenses.tar.gz`, the APK, `web-licenses.txt`, `SOURCE.txt`
and `SHA256SUMS`. Keep matching source available for every distributed version;
do not delete release tags or source archives while distributing those binaries.

Both containers include OCI license, repository and revision labels. The backend
serves its license page at `/legal/index.html`; the frontend and APK include
`assets/legal/index.html`. The sign-in screen and account navigation link to it.
Release builds offer the source archive for their exact commit.

## Building a modified distribution

Publish the complete source, including your modifications and build instructions,
somewhere recipients and network users can download it. Both Dockerfiles accept:

- `SOURCE_REVISION`: the full 40-character commit SHA being built.
- `SOURCE_REPOSITORY`: the public repository URL (defaults to the upstream GitHub
  repository). For GitHub repositories the source URL is derived from the SHA.
- `SOURCE_URL`: an explicit HTTP(S) URL for your corresponding source archive,
  for non-GitHub hosting or another download location.

Build the frontend Dockerfile from the repository root:

```sh
docker build -f src/LuminaPath.MobileApp/Dockerfile -t luminapath-frontend:local .
```

Pass the same variables in the environment when running `npm run build` for an
APK. For a direct .NET publish, run `node scripts/prepare-legal.mjs` with the output
directory set to the published `wwwroot/legal` directory and those environment
variables set. Ordinary development builds label their source as a development
checkout; an upstream link alone does not describe unpublished modifications.

## Manual GHCR / Azure publishing

Use **Actions → Manual GHCR and Azure Release → Run workflow** on the desired ref.
GHCR uses the workflow's `GITHUB_TOKEN` with package write permission. Azure
deployment additionally requires `AZURE_WEBAPP_NAME` and `AZURE_CREDENTIALS`.
If `AZURE_WEBAPP_NAME` is unset, the workflow only publishes images. Set GHCR
package visibility separately when public access is intended.

## Verification

Download a release and verify `SHA256SUMS` (`sha256sum -c SHA256SUMS` on Linux).
Confirm the license page loads and its source download matches `SOURCE.txt`.
Check that source and container registries can be accessed without your account.
The APK workflow checks that its assets include the AGPL text, notices, extracted
web licenses and the expected source revision before uploading it.
