import { readFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const manifest = JSON.parse(readFileSync(process.argv[2] || resolve(root, 'src/LuminaPath/bin/Release/net10.0/LuminaPath.deps.json'), 'utf8'));
const expected = JSON.parse(readFileSync(resolve(root, 'licenses/backend-packages.json'), 'utf8')).sort();
const actual = Object.entries(manifest.libraries).filter(([, value]) => value.type === 'package').map(([name]) => name).sort();
// Publish omits design-time packages that are present in the development build.
// Extra notices are harmless; every package actually shipped must be covered.
const missing = actual.filter(name => !expected.includes(name));
if (missing.length) {
  throw new Error(`Missing backend notices: ${missing.join(', ')}. Refresh and review with scripts/update-backend-licenses.py.`);
}
console.log(`License inventory covers all ${actual.length} backend packages.`);
