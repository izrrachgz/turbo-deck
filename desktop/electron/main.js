'use strict';

const { app, BrowserWindow, Menu, Tray, dialog, ipcMain, nativeImage, protocol, shell } = require('electron');
const { spawn } = require('node:child_process');
const { randomBytes } = require('node:crypto');
const { EventEmitter } = require('node:events');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const readline = require('node:readline');
const { HubConnectionBuilder, LogLevel } = require('@microsoft/signalr');
const { assertTrustedSender } = require('./ipc-trust');
const { createMediaProtocolHandler, MEDIA_PROTOCOL_PRIVILEGES } = require('./media-protocol');
const { createUpdateService } = require('./update-service');
const { createDeckExplorerUrl, createVisualizerUrl } = require('./visualizer-url');

const API_CHANNEL = 'backend:request';
const BACKEND_RESTART_CHANNEL = 'backend:restart';
const UPLOAD_CHANNEL = 'backend:upload-file';
const MEDIA_UPLOAD_CHANNEL = 'backend:upload-media';
const VISUALIZER_OPEN_CHANNEL = 'visualizer:open';
const DECK_OPEN_CHANNEL = 'deck:open';
const PLAYBACK_QUEUE_CHANGED_CHANNEL = 'playback:queue-changed';
const LAN_ACCESS_CHANNEL = 'network:create-lan-access';
const FIREWALL_CHANNEL = 'network:configure-firewall';
const OPEN_EXTERNAL_CHANNEL = 'desktop:open-external';
const APP_INFO_CHANNEL = 'desktop:app-info';
const APP_LICENSE_CHANNEL = 'desktop:license';
const APP_QUIT_CHANNEL = 'desktop:quit';
const UPDATE_STATUS_CHANNEL = 'updates:status';
const UPDATE_CHECK_CHANNEL = 'updates:check';
const UPDATE_DOWNLOAD_CHANNEL = 'updates:download';
const UPDATE_APPLY_CHANNEL = 'updates:apply';
const MEDIA_PROTOCOL_SCHEME = 'turbo-deck-media';
const MAX_API_REQUEST_BYTES = 1024 * 1024;
const MAX_API_RESPONSE_CHARACTERS = 16 * 1024 * 1024;
const API_ROUTE_RULES = Object.freeze({
  GET: [
    /^\/health\/(?:live|ready)$/,
    /^\/files(?:\?limit=\d{1,3})?$/,
    /^\/files\/\d+$/,
    /^\/system\/network$/,
    /^\/system\/database$/,
    /^\/api\/v1\/profiles$/,
    /^\/api\/v1\/profiles\/\d+\/pages$/,
    /^\/api\/v1\/pages\/\d+\/grid$/,
    /^\/api\/v1\/settings$/,
    /^\/api\/v1\/media-assets(?:\?limit=\d{1,3})?$/,
    /^\/api\/v1\/playback\/queue$/,
    /^\/api\/v1\/twitch\/(?:status|rewards|bindings)$/
  ],
  POST: [
    /^\/system\/network\/api-key\/rotate$/,
    /^\/system\/database\/(?:test|switch)$/,
    /^\/api\/v1\/profiles$/,
    /^\/api\/v1\/profiles\/\d+\/pages$/,
    /^\/api\/v1\/playback\/(?:queue|complete|clear)$/,
    /^\/api\/v1\/playback\/queue\/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\/move$/,
    /^\/api\/v1\/twitch\/(?:device-authorization|device-authorization\/poll|disconnect)$/
  ],
  PUT: [
    /^\/system\/network$/,
    /^\/api\/v1\/pages\/\d+\/cells\/[0-3]\/[0-7]$/,
    /^\/api\/v1\/settings$/,
    /^\/api\/v1\/twitch\/bindings\/[A-Za-z0-9_-]{1,128}$/
  ],
  DELETE: [
    /^\/files\/\d+$/,
    /^\/api\/v1\/playback\/queue\/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$/,
    /^\/api\/v1\/twitch\/bindings\/[A-Za-z0-9_-]{1,128}$/
  ]
});
const MIME_TYPES = Object.freeze({
  '.csv': 'text/csv', '.gif': 'image/gif', '.jpeg': 'image/jpeg', '.jpg': 'image/jpeg',
  '.m4a': 'audio/mp4', '.md': 'text/markdown', '.mp3': 'audio/mpeg', '.mp4': 'video/mp4', '.ogg': 'audio/ogg', '.pdf': 'application/pdf',
  '.png': 'image/png', '.txt': 'text/plain', '.wav': 'audio/wav', '.webp': 'image/webp',
  '.webm': 'video/webm', '.zip': 'application/zip'
});
const instanceLock = app.requestSingleInstanceLock();

protocol.registerSchemesAsPrivileged([{
  scheme: MEDIA_PROTOCOL_SCHEME,
  privileges: MEDIA_PROTOCOL_PRIVILEGES
}]);

app.enableSandbox();

let mainWindow;
let tray;
let backendProcess;
let backendRealtimeConnection;
let backendRealtimeRetryTimer;
let updateService;
let backendUrl;
let backendAddresses = [];
let quitting = false;
const backendToken = randomBytes(32).toString('base64url');
const backendLifecycle = new EventEmitter();

if (!instanceLock) {
  app.quit();
} else {
  app.on('second-instance', () => {
    showMainWindow();
  });
  app.whenReady().then(startApplication).catch(showFatalStartupError);
}

async function startApplication() {
  app.setAppUserModelId('com.turbodeck.desktop');
  updateService = createUpdateService(app, async () => {
    quitting = true;
    await stopBackend();
    backendProcess = undefined;
  }, signedUpdateChannelEnabled());
  registerIpcHandlers();
  await startBackend();
  protocol.handle(MEDIA_PROTOCOL_SCHEME, createMediaProtocolHandler(() => backendUrl, () => backendToken));
  createWindow();
  void startPlaybackRealtime();
  createTray();
  app.on('activate', () => {
    showMainWindow();
  });
}

function rendererIndexPath() {
  return app.isPackaged
    ? path.join(app.getAppPath(), 'ui', 'index.html')
    : path.resolve(__dirname, '..', 'dist-dev', 'turbo-deck-ui', 'browser', 'index.html');
}

function signedUpdateChannelEnabled() {
  if (!app.isPackaged) return false;
  try {
    const metadata = JSON.parse(fs.readFileSync(path.join(app.getAppPath(), 'package.json'), 'utf8'));
    return metadata.turboDeckSignedRelease === true;
  } catch {
    return false;
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
    'APPDATA', 'DOTNET_ROOT', 'HOME', 'LANG', 'LC_ALL', 'LOCALAPPDATA', 'Path', 'PATH', 'PATHEXT', 'PROGRAMDATA',
    'SystemRoot', 'TEMP', 'TMP', 'TMPDIR', 'USERPROFILE', 'WINDIR', 'XDG_CACHE_HOME', 'XDG_CONFIG_HOME', 'XDG_DATA_HOME'
  ];
  const environment = Object.fromEntries(allowedVariables
    .filter(name => typeof process.env[name] === 'string')
    .map(name => [name, process.env[name]]));
  return { ...environment, ...applicationVariables, DOTNET_EnableDiagnostics: '0', COMPlus_EnableDiagnostics: '0' };
}

function resolveBackendLaunch() {
  if (app.isPackaged) {
    const executable = process.platform === 'win32' ? 'TurboDeck.Backend.exe' : 'TurboDeck.Backend';
    return { command: path.join(process.resourcesPath, 'backend', executable), args: [] };
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
  backendAddresses = [];
  backendProcess = spawn(launch.command, [...launch.args, `--ParentPid=${process.pid}`], {
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
        resolve({
          url: readyUrl,
          addresses: Array.isArray(message.addresses) ? message.addresses.filter(value => typeof value === 'string') : []
        });
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
      const wasReady = Boolean(backendUrl);
      backendUrl = undefined;
      backendProcess = undefined;
      if (!wasReady) reject(new Error(`Backend exited during startup with code ${code}.`));
      else if (code === 23 && !quitting) void restartBackend();
      else if (!quitting) showFatalStartupError(new Error(`The backend stopped unexpectedly with code ${code}.`));
    });
  });
  backendUrl = ready.url;
  backendAddresses = ready.addresses;
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
    await startPlaybackRealtime();
    backendLifecycle.emit('restart-completed');
  } catch (error) {
    backendLifecycle.emit('restart-failed', error);
    console.error(error);
  }
}

function createBackendRestartWaiter(timeoutMilliseconds = 45_000) {
  let active = true;
  let timeout;
  let completed;
  let failed;
  const cleanup = () => {
    if (!active) return;
    active = false;
    clearTimeout(timeout);
    backendLifecycle.off('restart-completed', completed);
    backendLifecycle.off('restart-failed', failed);
  };
  const promise = new Promise((resolve, reject) => {
    completed = () => {
      cleanup();
      resolve();
    };
    failed = error => {
      cleanup();
      reject(error instanceof Error ? error : new Error(String(error)));
    };
    backendLifecycle.once('restart-completed', completed);
    backendLifecycle.once('restart-failed', failed);
    timeout = setTimeout(() => failed(new Error('The backend restart did not finish within 45 seconds.')), timeoutMilliseconds);
  });
  return { promise, cancel: cleanup };
}

async function startPlaybackRealtime() {
  await stopPlaybackRealtime();
  if (!backendUrl || quitting) return;

  const hubUrl = new URL('/hubs/realtime', backendUrl);
  hubUrl.searchParams.set('clientId', 'electron-desktop');
  const connection = new HubConnectionBuilder()
    .withUrl(hubUrl.toString(), { headers: { 'X-Turbo-Deck-Control-Token': backendToken } })
    .withAutomaticReconnect([0, 1000, 2500, 5000, 10000])
    .configureLogging(LogLevel.Warning)
    .build();
  backendRealtimeConnection = connection;
  connection.on('Receive', message => {
    if (['playback.requested', 'playback.queue.changed', 'playback.stop.requested'].includes(message?.name)) {
      notifyPlaybackQueueChanged();
    }
  });
  connection.onreconnected(async () => {
    try {
      await connection.invoke('JoinGroup', 'playback');
      notifyPlaybackQueueChanged();
    } catch (error) {
      console.error('[realtime] Failed to restore the playback subscription.', error);
    }
  });
  connection.onclose(error => {
    if (backendRealtimeConnection !== connection) return;
    backendRealtimeConnection = undefined;
    if (error) console.error('[realtime] Playback subscription closed.', error);
    schedulePlaybackRealtimeRetry();
  });

  try {
    await connection.start();
    if (backendRealtimeConnection !== connection) {
      await connection.stop();
      return;
    }
    await connection.invoke('JoinGroup', 'playback');
    notifyPlaybackQueueChanged();
  } catch (error) {
    if (backendRealtimeConnection === connection) backendRealtimeConnection = undefined;
    console.error('[realtime] Playback subscription failed.', error);
    schedulePlaybackRealtimeRetry();
  }
}

async function stopPlaybackRealtime() {
  if (backendRealtimeRetryTimer) clearTimeout(backendRealtimeRetryTimer);
  backendRealtimeRetryTimer = undefined;
  const connection = backendRealtimeConnection;
  backendRealtimeConnection = undefined;
  if (!connection) return;
  try { await connection.stop(); }
  catch (error) { console.error('[realtime] Failed to stop the playback subscription.', error); }
}

function schedulePlaybackRealtimeRetry() {
  if (backendRealtimeRetryTimer || quitting || !backendUrl) return;
  backendRealtimeRetryTimer = setTimeout(() => {
    backendRealtimeRetryTimer = undefined;
    void startPlaybackRealtime();
  }, 2000);
  backendRealtimeRetryTimer.unref();
}

function notifyPlaybackQueueChanged() {
  if (mainWindow && !mainWindow.isDestroyed()) mainWindow.webContents.send(PLAYBACK_QUEUE_CHANGED_CHANNEL);
}

function createWindow() {
  const icon = applicationIcon();
  mainWindow = new BrowserWindow({
    width: 1280,
    height: 820,
    minWidth: 920,
    minHeight: 640,
    show: false,
    backgroundColor: '#080b0d',
    icon,
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
  mainWindow.on('close', event => {
    if (quitting) return;
    event.preventDefault();
    mainWindow?.hide();
  });
  mainWindow.on('closed', () => { mainWindow = undefined; });
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

function createTray() {
  const icon = applicationIcon().resize({ width: process.platform === 'darwin' ? 18 : 20, height: process.platform === 'darwin' ? 18 : 20 });
  if (process.platform === 'darwin') icon.setTemplateImage(true);
  tray = new Tray(icon);
  tray.setToolTip('Turbo Deck');
  tray.setContextMenu(Menu.buildFromTemplate([
    { label: 'Open Turbo Deck', click: showMainWindow },
    { label: 'Hide window', click: () => mainWindow?.hide() },
    { type: 'separator' },
    {
      label: 'Quit Turbo Deck',
      click: () => {
        quitting = true;
        app.quit();
      }
    }
  ]));
  tray.on('click', () => {
    if (mainWindow?.isVisible()) mainWindow.hide();
    else showMainWindow();
  });
}

function showMainWindow() {
  if (!mainWindow) {
    createWindow();
    return;
  }
  if (mainWindow.isMinimized()) mainWindow.restore();
  mainWindow.show();
  mainWindow.focus();
}

function applicationIcon() {
  const icon = nativeImage.createFromPath(path.join(__dirname, 'assets', 'turbo-deck.png'));
  if (icon.isEmpty()) throw new Error('The Turbo Deck application icon is unavailable.');
  return icon;
}

function registerIpcHandlers() {
  ipcMain.handle(API_CHANNEL, async (event, request) => {
    assertTrustedSender(event, mainWindow?.webContents);
    const method = String(request?.method ?? 'GET').toUpperCase();
    const apiPath = String(request?.path ?? '');
    const routeRules = API_ROUTE_RULES[method];
    if (!routeRules || !routeRules.some(rule => rule.test(apiPath))) throw new Error('Blocked invalid backend request.');
    const body = request?.body === undefined ? undefined : JSON.stringify(request.body);
    if (body && Buffer.byteLength(body, 'utf8') > MAX_API_REQUEST_BYTES) throw new Error('Backend request is too large.');
    return backendRequest(apiPath, { method, body, headers: body ? { 'Content-Type': 'application/json' } : undefined });
  });

  ipcMain.handle(BACKEND_RESTART_CHANNEL, async event => {
    assertTrustedSender(event, mainWindow?.webContents);
    const waiter = createBackendRestartWaiter();
    try {
      await backendRequest('/system/restart', { method: 'POST' });
      await waiter.promise;
    } catch (error) {
      waiter.cancel();
      throw error;
    }
  });

  ipcMain.handle(UPLOAD_CHANNEL, async (event, request) => {
    assertTrustedSender(event, mainWindow?.webContents);
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

  ipcMain.handle(MEDIA_UPLOAD_CHANNEL, async (event, request) => {
    assertTrustedSender(event, mainWindow?.webContents);
    const mediaKind = request?.mediaKind ?? 'any';
    if (!['any', 'image'].includes(mediaKind)) throw new Error('The requested media selection type is not supported.');
    const result = await dialog.showOpenDialog(mainWindow, {
      title: mediaKind === 'image' ? 'Add thumbnail image' : 'Add media',
      properties: ['openFile'],
      filters: mediaKind === 'image'
        ? [{ name: 'Images', extensions: ['jpg', 'jpeg', 'png', 'gif', 'webp'] }]
        : [{ name: 'Web media', extensions: ['jpg', 'jpeg', 'png', 'gif', 'webp', 'mp3', 'wav', 'ogg', 'm4a', 'mp4', 'webm'] }]
    });
    if (result.canceled || result.filePaths.length === 0) return undefined;
    const filePath = result.filePaths[0];
    const extension = path.extname(filePath).toLowerCase();
    const contentType = MIME_TYPES[extension];
    if (!contentType) throw new Error('The selected media type is not supported.');
    const blob = await fs.openAsBlob(filePath, { type: contentType });
    const form = new FormData();
    form.append('File', blob, path.basename(filePath));
    if (typeof request?.displayName === 'string' && request.displayName.length <= 160) {
      form.append('DisplayName', request.displayName);
    }
    return backendRequest('/api/v1/media-assets', { method: 'POST', body: form });
  });

  ipcMain.handle(VISUALIZER_OPEN_CHANNEL, async event => {
    assertTrustedSender(event, mainWindow?.webContents);
    await shell.openExternal(createVisualizerUrl(backendUrl));
  });

  ipcMain.handle(DECK_OPEN_CHANNEL, async event => {
    assertTrustedSender(event, mainWindow?.webContents);
    await shell.openExternal(createDeckExplorerUrl(backendUrl));
  });

  ipcMain.handle(LAN_ACCESS_CHANNEL, async event => {
    assertTrustedSender(event, mainWindow?.webContents);
    const access = await backendRequest('/system/network/api-key/rotate', { method: 'POST' });
    return { apiKey: access.apiKey, baseUrls: networkBaseUrls() };
  });

  ipcMain.handle(FIREWALL_CHANNEL, async (event, request) => {
    assertTrustedSender(event, mainWindow?.webContents);
    if (process.platform !== 'win32') throw new Error('Windows Firewall administration is available only on Windows.');
    const port = Number(request?.port);
    const action = String(request?.action ?? '').toLowerCase();
    if (!Number.isInteger(port) || port < 1 || port > 65535 || !['add', 'remove'].includes(action)) {
      throw new Error('Invalid firewall request.');
    }
    const helperPath = app.isPackaged
      ? path.join(process.resourcesPath, 'helpers', 'configure-firewall.ps1')
      : path.join(__dirname, 'helpers', 'configure-firewall.ps1');
    if (!fs.existsSync(helperPath)) throw new Error('The firewall helper is unavailable.');
    await runProcess('powershell.exe', [
      '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', helperPath, '-Port', String(port), '-Action', action
    ]);
    return { applied: true };
  });

  ipcMain.handle(OPEN_EXTERNAL_CHANNEL, async (event, value) => {
    assertTrustedSender(event, mainWindow?.webContents);
    const target = new URL(String(value));
    const trustedTwitch = target.protocol === 'https:' && ['www.twitch.tv', 'dev.twitch.tv'].includes(target.hostname);
    const trustedRepository = target.protocol === 'https:' && target.hostname === 'github.com' &&
      (target.pathname === '/izrrachgz/turbo-deck' || target.pathname.startsWith('/izrrachgz/turbo-deck/issues'));
    const trustedDonation = target.toString() === 'https://www.paypal.com/donate/?hosted_button_id=5M8HPH943MVV4';
    if (!trustedTwitch && !trustedRepository && !trustedDonation) {
      throw new Error('Blocked untrusted external URL.');
    }
    await shell.openExternal(target.toString());
  });

  ipcMain.handle(APP_INFO_CHANNEL, event => {
    assertTrustedSender(event, mainWindow?.webContents);
    return {
      version: app.getVersion(),
      electronVersion: process.versions.electron ?? '',
      chromiumVersion: process.versions.chrome ?? '',
      nodeVersion: process.versions.node ?? '',
      platform: process.platform,
      architecture: process.arch,
      packaged: app.isPackaged
    };
  });

  ipcMain.handle(APP_LICENSE_CHANNEL, async event => {
    assertTrustedSender(event, mainWindow?.webContents);
    const licensePath = app.isPackaged
      ? path.join(process.resourcesPath, 'LICENSE.txt')
      : path.resolve(__dirname, '..', '..', 'LICENSE');
    const text = await fs.promises.readFile(licensePath, 'utf8');
    if (text.length > 64 * 1024) throw new Error('The license document is unexpectedly large.');
    return text;
  });

  ipcMain.handle(APP_QUIT_CHANNEL, event => {
    assertTrustedSender(event, mainWindow?.webContents);
    quitting = true;
    app.quit();
  });

  ipcMain.handle(UPDATE_STATUS_CHANNEL, event => {
    assertTrustedSender(event, mainWindow?.webContents);
    return updateService.getStatus();
  });
  ipcMain.handle(UPDATE_CHECK_CHANNEL, event => {
    assertTrustedSender(event, mainWindow?.webContents);
    return updateService.check();
  });
  ipcMain.handle(UPDATE_DOWNLOAD_CHANNEL, event => {
    assertTrustedSender(event, mainWindow?.webContents);
    return updateService.download();
  });
  ipcMain.handle(UPDATE_APPLY_CHANNEL, event => {
    assertTrustedSender(event, mainWindow?.webContents);
    updateService.apply();
  });
}

function backendBaseUrls() {
  const urls = new Set();
  for (const value of backendAddresses) {
    let address;
    try { address = new URL(value); }
    catch { continue; }
    if (['0.0.0.0', '[::]', '::'].includes(address.hostname)) {
      for (const interfaces of Object.values(os.networkInterfaces())) {
        for (const network of interfaces ?? []) {
          if (network.family !== 'IPv4' || network.internal) continue;
          const concrete = new URL(address);
          concrete.hostname = network.address;
          urls.add(concrete.toString());
        }
      }
    } else {
      urls.add(address.toString());
    }
  }
  urls.delete(backendUrl);
  return [backendUrl, ...[...urls].filter(Boolean).sort()].filter(Boolean);
}

function networkBaseUrls() {
  return backendBaseUrls().filter(value => {
    const hostname = new URL(value).hostname;
    return !['127.0.0.1', 'localhost', '[::1]', '::1'].includes(hostname);
  });
}

function runProcess(command, args) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, { windowsHide: true, stdio: ['ignore', 'pipe', 'pipe'] });
    let errorOutput = '';
    child.stderr.setEncoding('utf8');
    child.stderr.on('data', chunk => { errorOutput += chunk; });
    child.once('error', reject);
    child.once('exit', code => {
      if (code === 0) resolve();
      else reject(new Error(errorOutput.trim() || `The helper exited with code ${code}.`));
    });
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
  if (!response.ok) throw new Error(payload?.detail ?? payload?.title ?? payload?.error ?? `Backend request failed (${response.status}).`);
  return payload;
}

async function stopBackend() {
  await stopPlaybackRealtime();
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

app.on('window-all-closed', () => {
  // The tray owns application lifetime. Use its Quit command or the About page to exit.
});
