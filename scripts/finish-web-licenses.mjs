import { existsSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const output = resolve(root, 'src/LuminaPath.MobileApp/www');
// Angular emits this beside browser/, so explicitly include it in nginx and APK assets.
const licenses = resolve(output, '3rdpartylicenses.txt');
if (existsSync(licenses)) {
  let text = readFileSync(licenses, 'utf8');
  // These npm packages declare MIT but omit standalone license files.
  const supplements = {
    'qrcode-generator': ['2.0.4', 'qrcode-generator-2.0.4.txt'],
    '@microsoft/signalr': ['9.0.6', 'signalr-9.0.6.txt'],
    '@ionic/angular': ['8.8.5', 'ionic-angular-8.8.5.txt'],
  };
  for (const [name, [version, file]] of Object.entries(supplements)) {
    const pkg = JSON.parse(readFileSync(resolve(root, 'src/LuminaPath.MobileApp/node_modules', name, 'package.json'), 'utf8'));
    if (pkg.version !== version) throw new Error(`Review and update the supplemental license for ${name}@${pkg.version}.`);
    text += `\n${'-'.repeat(80)}\n${name}@${version}\n\n${readFileSync(resolve(root, 'licenses', file), 'utf8')}`;
  }
  writeFileSync(resolve(output, 'browser/assets/legal/web-licenses.txt'), text);
  const metadataPath = resolve(output, 'browser/assets/legal/source.json');
  const metadata = JSON.parse(readFileSync(metadataPath, 'utf8'));
  metadata.web = true;
  writeFileSync(metadataPath, `${JSON.stringify(metadata, null, 2)}\n`);
} else {
  throw new Error('Angular did not emit 3rdpartylicenses.txt; production releases require license extraction.');
}
