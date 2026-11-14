'use strict';

const assert = require('node:assert/strict');
const path = require('node:path');
const test = require('node:test');
const { createBackendLaunch, findMissingPackagedBackendPaths } = require('./backend-launch');

test('packaged backend launches from the directory containing its configuration and resources', () => {
  const resourcesPath = path.resolve('install', 'resources');
  const launch = createBackendLaunch(true, 'win32', resourcesPath, __dirname);

  assert.equal(launch.command, path.join(resourcesPath, 'backend', 'TurboDeck.Backend.exe'));
  assert.equal(launch.workingDirectory, path.join(resourcesPath, 'backend'));
  assert.deepEqual(launch.args, []);
});

test('development backend launches from the API project directory', () => {
  const launch = createBackendLaunch(false, 'win32', '', __dirname);

  assert.equal(launch.command, 'dotnet');
  assert.equal(launch.workingDirectory, path.resolve(__dirname, '..', '..', 'backend', 'Api'));
  assert.equal(launch.args.at(-1), '--');
});

test('packaged backend validation reports missing configuration and browser resources', () => {
  const launch = createBackendLaunch(true, 'win32', path.resolve('install', 'resources'), __dirname);
  const available = new Set([launch.command, path.join(launch.workingDirectory, 'appsettings.json')]);
  const missing = findMissingPackagedBackendPaths(launch, value => available.has(value));

  assert.equal(missing.includes(launch.command), false);
  assert.equal(missing.includes(path.join(launch.workingDirectory, 'appsettings.Production.json')), true);
  assert.equal(missing.includes(path.join(launch.workingDirectory, 'wwwroot', 'deck', 'index.html')), true);
  assert.equal(missing.includes(path.join(launch.workingDirectory, 'wwwroot', 'visualizer', 'index.html')), true);
});
