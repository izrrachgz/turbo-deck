'use strict';

function createUpdateService(app, beforeApply, updatesEnabled, updater) {
  const autoUpdater = updater ?? require('electron-updater').autoUpdater;
  const disabledPhase = app.isPackaged ? 'test' : 'development';
  const disabledMessage = app.isPackaged
    ? 'This unsigned local test build cannot download or apply updates.'
    : 'Update checks are available only in an installed signed release.';
  let state = Object.freeze({
    phase: updatesEnabled ? 'idle' : disabledPhase,
    currentVersion: app.getVersion(),
    availableVersion: null,
    progressPercent: null,
    message: updatesEnabled ? null : disabledMessage
  });

  function update(values) {
    state = Object.freeze({ ...state, ...values });
  }

  autoUpdater.autoDownload = false;
  autoUpdater.autoInstallOnAppQuit = false;
  autoUpdater.allowPrerelease = false;

  autoUpdater.on('checking-for-update', () => update({ phase: 'checking', message: null, progressPercent: null }));
  autoUpdater.on('update-available', info => update({
    phase: 'available',
    availableVersion: info.version,
    message: null,
    progressPercent: 0
  }));
  autoUpdater.on('update-not-available', () => update({
    phase: 'current',
    availableVersion: null,
    message: 'This installation is up to date.',
    progressPercent: null
  }));
  autoUpdater.on('download-progress', progress => update({
    phase: progress.percent >= 100 ? 'verifying' : 'downloading',
    progressPercent: Math.max(0, Math.min(100, Math.round(progress.percent))),
    message: null
  }));
  autoUpdater.on('update-downloaded', info => update({
    phase: 'ready',
    availableVersion: info.version,
    progressPercent: 100,
    message: 'The update passed manifest and package integrity verification.'
  }));
  autoUpdater.on('error', error => update({
    phase: 'error',
    progressPercent: null,
    message: safeError(error)
  }));

  return Object.freeze({
    getStatus() {
      return state;
    },
    async check() {
      if (!updatesEnabled) return state;
      update({ phase: 'checking', message: null, progressPercent: null });
      try {
        await autoUpdater.checkForUpdates();
      } catch (error) {
        update({ phase: 'error', message: safeError(error), progressPercent: null });
      }
      return state;
    },
    async download() {
      if (state.phase !== 'available') throw new Error('No verified update is available to download.');
      update({ phase: 'downloading', progressPercent: 0, message: null });
      try {
        await autoUpdater.downloadUpdate();
      } catch (error) {
        update({ phase: 'error', message: safeError(error), progressPercent: null });
      }
      return state;
    },
    async apply() {
      if (state.phase !== 'ready') throw new Error('The update has not completed verification.');
      await beforeApply();
      autoUpdater.quitAndInstall(false, true);
    }
  });
}

function safeError(error) {
  const message = error instanceof Error ? error.message : String(error);
  if (/app-update\.ya?ml|publish configuration|update information/i.test(message)) {
    return 'This test build does not have a configured signed update channel.';
  }
  return message.slice(0, 500);
}

module.exports = { createUpdateService };
