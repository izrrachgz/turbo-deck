'use strict';

const path = require('node:path');

const REQUIRED_PACKAGED_BACKEND_PATHS = Object.freeze([
  'appsettings.json',
  'appsettings.Production.json',
  'locales',
  path.join('wwwroot', 'deck', 'index.html'),
  path.join('wwwroot', 'visualizer', 'index.html')
]);

function createBackendLaunch(isPackaged, platform, resourcesPath, electronDirectory) {
  if (isPackaged) {
    const workingDirectory = path.join(resourcesPath, 'backend');
    const executable = platform === 'win32' ? 'TurboDeck.Backend.exe' : 'TurboDeck.Backend';
    return Object.freeze({
      command: path.join(workingDirectory, executable),
      args: [],
      workingDirectory,
      packaged: true
    });
  }

  const project = path.resolve(electronDirectory, '..', '..', 'backend', 'Api', 'Api.csproj');
  return Object.freeze({
    command: 'dotnet',
    args: ['run', '--no-launch-profile', '--project', project, '--'],
    workingDirectory: path.dirname(project),
    packaged: false
  });
}

function findMissingPackagedBackendPaths(launch, existsSync) {
  if (!launch.packaged) return [];
  const candidates = [launch.command, ...REQUIRED_PACKAGED_BACKEND_PATHS.map(value => path.join(launch.workingDirectory, value))];
  return candidates.filter(value => !existsSync(value));
}

module.exports = { createBackendLaunch, findMissingPackagedBackendPaths };
