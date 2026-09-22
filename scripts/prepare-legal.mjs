import { cpSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const output = resolve(process.argv[2] || resolve(root, 'src/LuminaPath.MobileApp/src/assets/legal'));
const revision = process.env.SOURCE_REVISION || '';
if (revision && !/^[0-9a-f]{40}$/i.test(revision)) {
  throw new Error('SOURCE_REVISION must be a full 40-character Git commit SHA.');
}
const repository = (process.env.SOURCE_REPOSITORY || 'https://github.com/dominictosku/LuminaPath').replace(/\/$/, '');
const url = process.env.SOURCE_URL || (revision ? `${repository}/archive/${revision}.tar.gz` : repository);
if (!['https:', 'http:'].includes(new URL(url).protocol)) {
  throw new Error('SOURCE_URL must use HTTP or HTTPS.');
}
mkdirSync(output, { recursive: true });
cpSync(resolve(root, 'legal'), output, { recursive: true });
cpSync(resolve(root, 'licenses'), resolve(output, 'licenses'), { recursive: true });
writeFileSync(resolve(output, 'LICENSE.txt'), readFileSync(resolve(root, 'LICENSE')));
writeFileSync(resolve(output, 'THIRD_PARTY_NOTICES.txt'), readFileSync(resolve(root, 'THIRD_PARTY_NOTICES.md')));
writeFileSync(resolve(output, 'source.json'), `${JSON.stringify({ url, revision, web: false }, null, 2)}\n`);
console.log(`Prepared license and source information in ${output}`);
