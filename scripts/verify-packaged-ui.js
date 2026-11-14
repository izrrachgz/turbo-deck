'use strict';

const fs = require('node:fs');
const path = require('node:path');
const { createRequire } = require('node:module');

const repositoryRoot = path.resolve(__dirname, '..');
const desktopRequire = createRequire(path.join(repositoryRoot, 'desktop', 'package.json'));
const asar = desktopRequire('@electron/asar');
const archivePath = process.argv[2];

if (!archivePath || !fs.existsSync(archivePath)) {
  throw new Error(`Packaged application archive was not found: ${archivePath || '(missing)'}`);
}

const indexHtml = asar.extractFile(archivePath, 'ui/index.html').toString('utf8');
const stylesheet = indexHtml.match(/<link rel="stylesheet" href="(styles-[^"]+\.css)">/)?.[1];

if (!stylesheet) {
  throw new Error('Packaged UI does not load its production stylesheet directly.');
}

if (/rel="stylesheet"[^>]+media="print"/.test(indexHtml) || indexHtml.includes('data-beasties-media')) {
  throw new Error('Packaged UI still relies on an inline script to activate its stylesheet.');
}

const stylesheetContents = asar.extractFile(archivePath, `ui/${stylesheet}`);
if (stylesheetContents.length === 0) {
  throw new Error(`Packaged UI stylesheet is empty: ${stylesheet}`);
}

console.log(`Verified packaged UI stylesheet ${stylesheet} (${stylesheetContents.length} bytes).`);
