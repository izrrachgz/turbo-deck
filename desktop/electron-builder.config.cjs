'use strict';

const path = require('node:path');

const runtime = process.env.TURBO_DECK_RUNTIME || 'win-x64';
const requireSigning = process.env.TURBO_DECK_REQUIRE_SIGNING === '1';
const externalSigning = process.env.TURBO_DECK_EXTERNAL_SIGNING === '1';
const officialRelease = process.env.TURBO_DECK_OFFICIAL_RELEASE === '1';

if (requireSigning && externalSigning) {
  throw new Error('Local certificate signing and external SignPath signing are mutually exclusive.');
}

module.exports = {
  appId: 'com.turbodeck.desktop',
  productName: 'Turbo Deck',
  asar: true,
  npmRebuild: false,
  forceCodeSigning: requireSigning,
  extraMetadata: { turboDeckSignedRelease: officialRelease },
  electronFuses: {
    runAsNode: false,
    enableCookieEncryption: true,
    enableNodeOptionsEnvironmentVariable: false,
    enableNodeCliInspectArguments: false,
    enableEmbeddedAsarIntegrityValidation: true,
    onlyLoadAppFromAsar: true
  },
  directories: { output: '../artifacts/installer' },
  files: [
    { from: 'electron', to: 'electron', filter: ['**/*'] },
    { from: 'dist/turbo-deck-ui/browser', to: 'ui', filter: ['**/*'] },
    'package.json'
  ],
  extraResources: [
    { from: path.join('..', 'artifacts', 'backend', runtime), to: 'backend', filter: ['**/*', '!*.pdb'] },
    { from: 'electron/helpers', to: 'helpers', filter: ['**/*'] },
    { from: '../LICENSE', to: 'LICENSE.txt' }
  ],
  publish: [{ provider: 'github', owner: 'izrrachgz', repo: 'turbo-deck', releaseType: 'release' }],
  win: {
    icon: 'electron/assets/turbo-deck.png',
    target: 'nsis',
    artifactName: 'Turbo-Deck-Setup-${version}-${arch}.${ext}'
  },
  nsis: {
    oneClick: false,
    perMachine: false,
    allowElevation: true,
    allowToChangeInstallationDirectory: true,
    createDesktopShortcut: true,
    createStartMenuShortcut: true,
    runAfterFinish: true,
    deleteAppDataOnUninstall: false
  },
  linux: {
    icon: 'electron/assets/turbo-deck.png',
    category: 'Utility',
    target: 'AppImage',
    artifactName: 'Turbo-Deck-${version}-${arch}.${ext}'
  },
  mac: {
    icon: 'electron/assets/turbo-deck.png',
    category: 'public.app-category.utilities',
    target: ['dmg', 'zip'],
    artifactName: 'Turbo-Deck-${version}-${arch}.${ext}'
  }
};
