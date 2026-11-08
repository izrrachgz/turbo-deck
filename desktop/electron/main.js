'use strict';

const { app, BrowserWindow, dialog, ipcMain, shell } = require('electron');
const { spawn } = require('node:child_process');
const { randomBytes } = require('node:crypto');
const fs = require('node:fs');
const path = require('node:path');
const readline = require('node:readline');
const { fileURLToPath } = require('node:url');

const API_CHANNEL = 'backend:request';
const UPLOAD_CHANNEL = 'backend:upload-file';
const APP_INFO_CHANNEL = 'desktop:app-info';
const MAX_API_REQUEST_BYTES = 1024 * 1024;
const MAX_API_RESPONSE_CHARACTERS = 16 * 1024 * 1024;
const API_ROUTE_RULES = Object.freeze({
  GET: [
    /^\/health\/(?:live|ready)$/,
    /^\/files(?:\?limit=\d{1,3})?$/,
    /^\/files\/\d+$/,
    /^\/system\/network$/
  ],
  POST: [
    /^\/system\/(?:restart|shutdown)$/,
    /^\/system\/network\/api-key\/rotate$/
  ],
  PUT: [/^\/system\/network$/],
  DELETE: [/^\/files\/\d+$/]
});
const MIME_TYPES = Object.freeze({
  '.csv': 'text/csv', '.gif': 'image/gif', '.jpeg': 'image/jpeg', '.jpg': 'image/jpeg',
  '.md': 'text/markdown', '.mp3': 'audio/mpeg', '.mp4': 'video/mp4', '.pdf': 'application/pdf',
  '.png': 'image/png', '.txt': 'text/plain', '.wav': 'audio/wav', '.webp': 'image/webp',
  '.zip': 'application/zip'
});
const instanceLock = app.requestSingleInstanceLock();

app.enableSandbox();

let mainWindow;
let backendProcess;
let backendUrl;
let quitting = false;
const backendToken = randomBytes(32).toString('base64url');

if (!instanceLock) {
  app.quit();
} else {
  app.on('second-instance', () => {
    if (!mainWindow) return;
    if (mainWindow.isMinimized()) mainWindow.restore();
    mainWindow.focus();
  });
  app.whenReady().then(startApplication).catch(showFatalStartupError);
}

async function startApplication() {
  registerIpcHandlers();
  await startBackend();
  createWindow();
  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
}

function rendererIndexPath() {
  return app.isPackaged
    ? path.join(app.getAppPath(), 'ui', 'index.html')
    : path.resolve(__dirname, '..', 'dist', 'turbo-deck-ui', 'browser', 'index.html');
}

function assertTrustedSender(event) {
  const frame = event.senderFrame;
  if (!frame || frame !== event.sender.mainFrame) throw new Error('Blocked IPC from an untrusted frame.');
  try {
    const senderPath = path.normalize(fileURLToPath(frame.url)).toLowerCase();
    const expectedPath = path.normalize(rendererIndexPath()).toLowerCase();
    if (senderPath !== expectedPath) throw new Error('Unexpected renderer path.');
  } catch {
    throw new Error('Blocked IPC from an untrusted renderer.');
  }
}

function createBackendEnvironment(dataDirectory) {
  const applicationVariables = {
    ASPNETCORE_ENVIRONMENT: app.isPackaged ? 'Production' : 'Development',
    DOTNET_CLI_TELEMETRY_OPTOUT: '1',
    TURBO_DECK_CONTROL_TOKEN: backendToken,
    TURBO_DECK_DATA_DIR: dataDirectory
  };
  if (!app.isPackaged) return { ...process.env, ...applicationVariables };

  const allowedVariables = [
    'APPDATA', 'DOTNET_ROOT', 'LOCALAPPDATA', 'Path', 'PATH', 'PATHEXT', 'PROGRAMDATA',
    'SystemRoot', 'TEMP', 'TMP', 'USERPROFILE', 'WINDIR'
  ];
  const environment = Object.fromEntries(allowedVariables
    .filter(name => typeof process.env[name] === 'string')
    .map(name => [name, process.env[name]]));
  return { ...environment, ...applicationVariables, DOTNET_EnableDiagnostics: '0', COMPlus_EnableDiagnostics: '0' };
}

function resolveBackendLaunch() {
  if (app.isPackaged) {
    return { command: path.join(process.resourcesPath, 'backend', 'TurboDeck.Backend.exe'), args: [] };
  }
  const project = path.resolve(__dirname, '..', '..', 'backend', 'Api', 'Api.csproj');
  return { command: 'dotnet', args: ['run', '--no-launch-profile', '--project', project, '--'] };
}

async function startBackend() {
  const launch = resolveBackendLaunch();
  if (app.isPackaged && !fs.existsSync(launch.command)) {
    throw new Error(`Packaged backend was not found at ${launch.command}`);
  }

  const dataDirectory = path.join(app.getPath('userData'), 'backend');
  fs.mkdirSync(dataDirectory, { recursive: true });
  backendUrl = undefined;
  backendProcess = spawn(launch.command, [...launch.args, '--Port=0', `--ParentPid=${process.pid}`], {
    windowsHide: true,
    stdio: ['ignore', 'pipe', 'pipe'],
    env: createBackendEnvironment(dataDirectory)
  });
  backendProcess.stderr.setEncoding('utf8');
  backendProcess.stderr.on('data', chunk => console.error(`[backend] ${chunk.trimEnd()}`));

  const ready = await new Promise((resolve, reject) => {
    const timeout = setTimeout(() => reject(new Error('The backend did not become ready within 30 seconds.')), 30_000);
    const lines = readline.createInterface({ input: backendProcess.stdout });
    lines.on('line', line => {
      try {
        const message = JSON.parse(line);
        if (message.event !== 'ready' || typeof message.url !== 'string') return;
        const readyUrl = normalizeReadyUrl(message.url);
        clearTimeout(timeout);
        lines.close();
        resolve(readyUrl);
      } catch {
        console.log(`[backend] ${line}`);
      }
    });
    backendProcess.once('error', error => {
      clearTimeout(timeout);
      reject(error);
    });
    backendProcess.once('exit', code => {
      clearTimeout(timeout);
      if (!backendUrl) reject(new Error(`Backend exited during startup with code ${code}.`));
      if (code === 23 && !quitting) void restartBackend();
    });
  });
  backendUrl = ready;
}

function normalizeReadyUrl(value) {
  const readyUrl = new URL(value);
  if (!['http:', 'https:'].includes(readyUrl.protocol) || !readyUrl.port) {
    throw new Error('The backend reported an invalid endpoint.');
  }
  if (['0.0.0.0', '[::]', '::'].includes(readyUrl.hostname)) readyUrl.hostname = '127.0.0.1';
  if (!['127.0.0.1', 'localhost', '[::1]', '::1'].includes(readyUrl.hostname)) {
    throw new Error('The backend did not expose a local control endpoint.');
  }
  return readyUrl.toString();
}

async function restartBackend() {
  try {
    await startBackend();
    mainWindow?.webContents.send('backend:restarted');
  } catch (error) {
    showFatalStartupError(error);
  }
}

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1280,
    height: 820,
    minWidth: 920,
    minHeight: 640,
    show: false,
    backgroundColor: '#080b0d',
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: true,
      nodeIntegration: false,
      sandbox: true,
      webSecurity: true,
      devTools: !app.isPackaged
    }
  });
  mainWindow.removeMenu();
  mainWindow.webContents.session.setPermissionCheckHandler(() => false);
  mainWindow.webContents.session.setPermissionRequestHandler((_contents, _permission, callback) => callback(false));
  mainWindow.once('ready-to-show', () => mainWindow.show());
  mainWindow.webContents.setWindowOpenHandler(({ url }) => {
    if (url === 'https://github.com/izrrachgz/turbo-deck') void shell.openExternal(url);
    return { action: 'deny' };
  });
  mainWindow.webContents.on('will-navigate', event => event.preventDefault());
  mainWindow.webContents.on('will-attach-webview', event => event.preventDefault());
  mainWindow.webContents.on('devtools-opened', () => {
    if (app.isPackaged) mainWindow?.webContents.closeDevTools();
  });
  void mainWindow.loadFile(rendererIndexPath());
}

function registerIpcHandlers() {
  ipcMain.handle(API_CHANNEL, async (event, request) => {
    assertTrustedSender(event);
    const method = String(request?.method ?? 'GET').toUpperCase();
    const apiPath = String(request?.path ?? '');
    const routeRules = API_ROUTE_RULES[method];
    if (!routeRules || !routeRules.some(rule => rule.test(apiPath))) throw new Error('Blocked invalid backend request.');
    const body = request?.body === undefined ? undefined : JSON.stringify(request.body);
    if (body && Buffer.byteLength(body, 'utf8') > MAX_API_REQUEST_BYTES) throw new Error('Backend request is too large.');
    return backendRequest(apiPath, { method, body, headers: body ? { 'Content-Type': 'application/json' } : undefined });
  });

  ipcMain.handle(UPLOAD_CHANNEL, async (event, request) => {
    assertTrustedSender(event);
    const result = await dialog.showOpenDialog(mainWindow, { title: 'Upload a file', properties: ['openFile'] });
    if (result.canceled || result.filePaths.length === 0) return undefined;
    const filePath = result.filePaths[0];
    const extension = path.extname(filePath).toLowerCase();
    const contentType = MIME_TYPES[extension] ?? 'application/octet-stream';
    const blob = await fs.openAsBlob(filePath, { type: contentType });
    const form = new FormData();
    form.append('File', blob, path.basename(filePath));
    if (typeof request?.purpose === 'string' && request.purpose.length <= 100) form.append('Purpose', request.purpose);
    return backendRequest('/files', { method: 'POST', body: form });
  });

  ipcMain.handle(APP_INFO_CHANNEL, event => {
    assertTrustedSender(event);
    return {
      version: app.getVersion(),
      electronVersion: process.versions.electron ?? '',
      chromiumVersion: process.versions.chrome ?? '',
      nodeVersion: process.versions.node ?? '',
      packaged: app.isPackaged
    };
  });
}

async function backendRequest(apiPath, init) {
  if (!backendUrl) throw new Error('The backend is not ready.');
  const headers = new Headers(init.headers);
  headers.set('X-Turbo-Deck-Control-Token', backendToken);
  const response = await fetch(new URL(apiPath, backendUrl), {
    ...init,
    headers,
    signal: AbortSignal.timeout(120_000)
  });
  const text = await response.text();
  if (text.length > MAX_API_RESPONSE_CHARACTERS) throw new Error('Backend response is too large.');
  let payload;
  try { payload = text ? JSON.parse(text) : undefined; }
  catch { payload = { title: text || `Backend request failed (${response.status}).` }; }
  if (!response.ok) throw new Error(payload?.title ?? payload?.error ?? `Backend request failed (${response.status}).`);
  return payload;
}

async function stopBackend() {
  if (!backendProcess || backendProcess.exitCode !== null || !backendUrl) return;
  try {
    await backendRequest('/system/shutdown', { method: 'POST' });
  } catch {
    // Parent-process monitoring is the final cleanup path.
  }
  setTimeout(() => {
    if (backendProcess && backendProcess.exitCode === null) backendProcess.kill();
  }, 3_000).unref();
}

function showFatalStartupError(error) {
  console.error(error);
  dialog.showErrorBox('Turbo Deck could not start', error instanceof Error ? error.message : String(error));
  app.quit();
}

app.on('before-quit', event => {
  quitting = true;
  if (backendProcess && backendProcess.exitCode === null) {
    event.preventDefault();
    stopBackend().finally(() => {
      backendProcess = undefined;
      app.quit();
    });
  }
});
