import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

const candidatePaths = [
  path.resolve(process.cwd(), 'node_modules/@angular/cli/lib/init.js'),
  path.resolve(process.cwd(), 'src/LuminaPath.MobileApp/node_modules/@angular/cli/lib/init.js'),
  path.resolve(__dirname, '../src/LuminaPath.MobileApp/node_modules/@angular/cli/lib/init.js'),
  path.resolve(__dirname, '../node_modules/@angular/cli/lib/init.js'),
];

const initFile = candidatePaths.find((p) => fs.existsSync(p));

if (!initFile) {
  // Mobile app node_modules not yet installed; exit cleanly
  process.exit(0);
}

let content = fs.readFileSync(initFile, 'utf8');
let modified = false;

// Angular CLI 14+ sets forceExit = false and relies on the Node.js event loop draining.
// In Angular 21 with @angular/build, internal worker pools (Piscina) and esbuild services
// maintain unclosed MessagePort and child process handles that prevent the Node.js event
// loop from draining naturally during one-shot `ng build` commands, causing the build to hang
// indefinitely after printing "Output location: ...".
// Setting forceExit = true and stopping esbuild cleanly when running `build` ensures
// graceful teardown and process.exit(exitCode) once bundle generation completes.
const patchMarker = '/* patched: forceExit for build */';
if (!content.includes(patchMarker)) {
  const targetExit = `.then((exitCode = 0) => {
    if (forceExit) {
        process.exit(exitCode);
    }
    process.exitCode = exitCode;
})`;

  const replacementExit = `.then(async (exitCode = 0) => {
    ${patchMarker}
    if (forceExit || process.argv.includes('build')) {
        try {
            const esbuild = require('esbuild');
            await esbuild.stop?.();
        } catch {}
        process.exit(exitCode);
    }
    process.exitCode = exitCode;
})`;

  if (content.includes(targetExit)) {
    content = content.replace(targetExit, replacementExit);
    modified = true;
  } else {
    // Fallback if target was already slightly altered
    const targetForceExit = 'let forceExit = false;';
    const replacementForceExit = `let forceExit = ${patchMarker} process.argv.includes("build") || false;`;
    if (content.includes(targetForceExit)) {
      content = content.replace(targetForceExit, replacementForceExit);
      modified = true;
    }
  }
}

if (modified) {
  fs.writeFileSync(initFile, content, 'utf8');
  console.log(`[patch-angular-cli] Patched ${initFile} for clean build exit.`);
}
