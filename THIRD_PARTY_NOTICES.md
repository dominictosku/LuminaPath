# Third-party notices

LuminaPath's original code is AGPL-3.0-only. The components below retain their
own copyrights and licenses; the project license does not replace them.

## Assets committed to this repository

| Component | Location | Upstream license |
| --- | --- | --- |
| Bootstrap 5.1.0 | `src/LuminaPath/wwwroot/bootstrap/` (CSS and source map) | MIT; full notice in `licenses/bootstrap-5.1.0.txt` |
| Tailwind CSS 3.4.0 output | `src/LuminaPath/wwwroot/app.min.css` | MIT; full notice in `licenses/tailwindcss-3.4.0.txt` |
| Gradle wrapper | `src/LuminaPath.MobileApp/android/gradle/wrapper/`, `gradlew`, `gradlew.bat` | Apache-2.0; see the JAR's `META-INF/LICENSE`, the script headers and `licenses/Apache-2.0.txt` |

The Bootstrap and Tailwind notices were retrieved from the upstream version tags
by `scripts/update-bundled-licenses.py`. Preserve the headers in bundled files.

## Dependencies included in builds

- `licenses/backend.txt` contains license and notice texts for the NuGet packages
  in the backend's Release dependency manifest. `licenses/backend-packages.json`
  records the package versions. Texts come from the installed packages or their
  pinned upstream repository commits, recorded beside each text.
- Angular's production build extracts JavaScript dependency notices. The npm
  postbuild step copies them to `assets/legal/web-licenses.txt` so they are included
  in both the frontend container and Capacitor APK. DOMPurify is used under its
  Apache-2.0 option. The MIT/Apache/BSD notices remain applicable to those components.
  Supplemental full MIT notices for `qrcode-generator` 2.0.4, `@microsoft/signalr`
  9.0.6 and `@ionic/angular` 8.8.5 are retained in `licenses/` and appended to the
  web bundle because their npm packages omit standalone license files.
- `licenses/android.txt` includes Apache-2.0 native dependencies, available embedded
  notices and Capacitor's MIT notices. `licenses/android-packages.json` records the
  resolved Maven coordinates, including dependency constraints. Test-only JUnit
  dependencies are not part of this release inventory.
- Base container images and operating-system packages retain their own license
  files and notices in their image layers. Do not strip those when redistributing.

Build and test tools have their own licenses even when they are not included in
the application. Package manifests and lockfiles identify those dependencies;
this notice is not a relicensing of them or of externally retrieved media.

## Updating notices

After changing dependencies, regenerate the applicable inventory and review all
new licenses. See `docs/05-runbooks/releases.md` for the commands. Keep any upstream
NOTICE files alongside license texts, and retain source/build instructions when
distributing a modified version. Historical dependency versions require their own
review if building and distributing older commits.
