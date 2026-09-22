"""Verify the legal assets and source identity inside a built APK."""
import json
import os
from pathlib import Path
import sys
import zipfile

root = Path(__file__).resolve().parents[1]
with zipfile.ZipFile(sys.argv[1]) as apk:
    base = "assets/public/assets/legal/"
    if apk.read(base + "LICENSE.txt") != (root / "LICENSE").read_bytes():
        raise ValueError("APK does not contain the project's complete AGPL license")
    for name in ("index.html", "source.js", "THIRD_PARTY_NOTICES.txt", "web-licenses.txt", "licenses/android.txt", "licenses/bootstrap-5.1.0.txt", "licenses/tailwindcss-3.4.0.txt"):
        if not apk.read(base + name).strip():
            raise ValueError(f"Empty APK legal asset: {name}")
    info = json.loads(apk.read(base + "source.json"))
    revision = os.environ.get("SOURCE_REVISION", "")
    if info["revision"] != revision or not info["web"]:
        raise ValueError("APK source revision or web notice metadata does not match this build")
print(f"Verified license assets and source revision in {sys.argv[1]}")
