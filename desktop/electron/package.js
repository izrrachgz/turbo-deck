'use strict';

const { spawnSync } = require('node:child_process');
const path = require('node:path');

const runtime = process.argv[2];
const publish = process.argv.includes('--publish');
const targets = {
  'win-x64': ['--win', 'nsis', '--x64'],
  'win-arm64': ['--win', 'nsis', '--arm64'],
  'linux-x64': ['--linux', 'AppImage', '--x64'],
  'linux-arm64': ['--linux', 'AppImage', '--arm64'],
  'osx-x64': ['--mac', 'dmg', 'zip', '--x64'],
  'osx-arm64': ['--mac', 'dmg', 'zip', '--arm64']
};

if (!Object.hasOwn(targets, runtime)) {
  console.error(`Unsupported Turbo Deck package runtime: ${runtime || '(missing)'}`);
  process.exit(2);
}

const cli = require.resolve('electron-builder/out/cli/cli.js');
const cliArguments = [cli, '--config', path.resolve(__dirname, '..', 'electron-builder.config.cjs'), ...targets[runtime]];
if (publish) cliArguments.push('--publish', 'always');
const result = spawnSync(process.execPath, cliArguments, {
  cwd: path.resolve(__dirname, '..'),
  env: { ...process.env, TURBO_DECK_RUNTIME: runtime },
  stdio: 'inherit'
});

process.exit(result.status ?? 1);
