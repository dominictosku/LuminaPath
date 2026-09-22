"""Generate native notices from Gradle's releaseRuntimeClasspath dependency report.

Run after `npm ci`, `npx cap sync android` and Gradle dependency resolution.
Only the reviewed Apache-2.0 Maven families below are accepted. A new family
requires a license review rather than silently inheriting the project's license.
"""
import argparse
import json
from pathlib import Path
import re
import xml.etree.ElementTree as ET
import zipfile
import io

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("report", type=Path)
parser.add_argument("--gradle-home", type=Path, default=Path.home() / ".gradle")
parser.add_argument("--check", action="store_true", help="Check the committed inventory without writing files")
args = parser.parse_args()
raw = args.report.read_bytes()
report = raw.decode("utf-16" if raw.startswith((b"\xff\xfe", b"\xfe\xff")) else "utf-8")
if "BUILD SUCCESSFUL" not in report or "FAILED" in report:
    raise ValueError("A successful, fully resolved Gradle report is required")
coordinates = set()
for line in report.splitlines():
    match = re.search(r"(?:\+---|\\---) ([\w.-]+):([\w.-]+):([\w.-]+)(?: -> ([\w.-]+))?", line)
    if match:
        coordinates.add((match[1], match[2], match[4] or match[3]))
if not coordinates:
    raise ValueError("No dependencies found")
if args.check:
    expected = json.loads((root / "licenses/android-packages.json").read_text(encoding="utf-8"))
    if expected != sorted(":".join(c) for c in coordinates):
        raise ValueError("Android dependencies changed. Refresh and review the native notices.")
    notices = (root / "licenses/android.txt").read_text(encoding="utf-8")
    for folder in (root / "src/LuminaPath.MobileApp/node_modules/@capacitor").iterdir():
        package = json.loads((folder / "package.json").read_text(encoding="utf-8"))
        if f"{package['name']}@{package['version']}\n" not in notices:
            raise ValueError(f"Refresh the notice for {package['name']}@{package['version']}")
    print(f"License inventory matches all {len(coordinates)} Maven coordinates and Capacitor versions")
    raise SystemExit(0)
sections = ["LuminaPath Android dependency licenses\n\nNative Maven dependencies (Apache-2.0):\n"]
cache = args.gradle_home / "caches/modules-2/files-2.1"


def archive_notices(archive, prefix):
    notices = []
    for name in sorted(archive.namelist()):
        basename = name.rsplit("/", 1)[-1].lower()
        if basename.startswith(("license", "notice", "copying")) and not name.endswith("/"):
            notices.append(f"\nSource: {prefix}!/{name}\n\n" + archive.read(name).decode("utf-8-sig"))
        elif name == "classes.jar":
            with zipfile.ZipFile(io.BytesIO(archive.read(name))) as nested:
                notices.extend(archive_notices(nested, f"{prefix}!/{name}"))
    return notices


for group, artifact, version in sorted(coordinates):
    coordinate = f"{group}:{artifact}:{version}"
    folder = cache / group / artifact / version
    pom = next(folder.glob("*/*.pom"), None)
    if pom is None:
        raise ValueError(f"Missing cached POM: {coordinate}")
    names = [node.text or "" for node in ET.parse(pom).iter() if node.tag.endswith("}license") for node in node if node.tag.endswith("}name")]
    reviewed = group.startswith("androidx.") or group in {"org.apache.cordova", "org.jetbrains", "org.jetbrains.kotlin", "org.jetbrains.kotlinx", "org.jspecify"} or coordinate == "com.google.guava:listenablefuture:1.0"
    if not reviewed or (names and not all("Apache" in name and "2.0" in name for name in names)):
        raise ValueError(f"Review the license of {coordinate}: {names}")
    sections.append(coordinate + "\n")
    for path in sorted(folder.glob("*/*")):
        if path.suffix in {".aar", ".jar"}:
            with zipfile.ZipFile(path) as archive:
                sections.extend(archive_notices(archive, coordinate))
sections.append("\n" + (root / "licenses/Apache-2.0.txt").read_text(encoding="utf-8"))
node_modules = root / "src/LuminaPath.MobileApp/node_modules"
for folder in sorted((node_modules / "@capacitor").iterdir()):
    package = json.loads((folder / "package.json").read_text(encoding="utf-8"))
    license_file = folder / "LICENSE"
    if not license_file.exists():
        raise ValueError(f"Missing Capacitor license: {folder.name}")
    sections.append(f"\n{'=' * 78}\n{package['name']}@{package['version']}\n\n" + license_file.read_text(encoding="utf-8"))
(root / "licenses/android.txt").write_text("".join(sections), encoding="utf-8")
(root / "licenses/android-packages.json").write_text(json.dumps(sorted(":".join(c) for c in coordinates), indent=2) + "\n", encoding="utf-8")
print(f"Collected notices for {len(coordinates)} Maven coordinates and Capacitor packages")
