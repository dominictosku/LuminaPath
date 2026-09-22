"""Download the licenses for the pinned vendored assets. Review version changes."""
from pathlib import Path
from urllib.request import urlopen

root = Path(__file__).resolve().parents[1]
sources = {
    "bootstrap-5.1.0.txt": "https://raw.githubusercontent.com/twbs/bootstrap/v5.1.0/LICENSE",
    "tailwindcss-3.4.0.txt": "https://raw.githubusercontent.com/tailwindlabs/tailwindcss/v3.4.0/LICENSE",
    "Apache-2.0.txt": "https://www.apache.org/licenses/LICENSE-2.0.txt",
    "ionic-angular-8.8.5.txt": "https://raw.githubusercontent.com/ionic-team/ionic-framework/v8.8.5/LICENSE",
    "signalr-9.0.6.txt": "https://raw.githubusercontent.com/dotnet/aspnetcore/v9.0.6/LICENSE.txt",
}
for name, url in sources.items():
    with urlopen(url, timeout=30) as response:
        (root / "licenses" / name).write_bytes(response.read())
    print(f"Updated {name} from {url}")

# This package declares MIT and supplies its copyright in the source header,
# but omits a standalone license file. Preserve that attribution with MIT's text.
mit = (root / "licenses/bootstrap-5.1.0.txt").read_text(encoding="utf-8")
permission = mit[mit.index("Permission is hereby granted"):]
(root / "licenses/qrcode-generator-2.0.4.txt").write_text(
    "qrcode-generator 2.0.4 — MIT License\nCopyright (c) 2009 Kazuhiko Arase\n"
    "Source: node_modules/qrcode-generator/dist/qrcode.mjs (copyright and license declaration)\n"
    "The word 'QR Code' is registered trademark of DENSO WAVE INCORPORATED.\n\n"
    + permission, encoding="utf-8")
