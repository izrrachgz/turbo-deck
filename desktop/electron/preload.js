'use strict';

const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('turboDeck', Object.freeze({
  request(method, path, body) {
    return ipcRenderer.invoke('backend:request', { method, path, body });
  },
  selectAndUploadFile(purpose) {
    return ipcRenderer.invoke('backend:upload-file', { purpose });
  },
  selectAndUploadMedia(displayName, mediaKind) {
    return ipcRenderer.invoke('backend:upload-media', { displayName, mediaKind });
  },
  openVisualizer() {
    return ipcRenderer.invoke('visualizer:open');
  },
  openDeckExplorer() {
    return ipcRenderer.invoke('deck:open');
  },
  onPlaybackQueueChanged(callback) {
    if (typeof callback !== 'function') throw new TypeError('A playback queue listener is required.');
    const listener = () => callback();
    ipcRenderer.on('playback:queue-changed', listener);
    return () => ipcRenderer.removeListener('playback:queue-changed', listener);
  },
  createLanAccess() {
    return ipcRenderer.invoke('network:create-lan-access');
  },
  configureFirewall(port, action) {
    return ipcRenderer.invoke('network:configure-firewall', { port, action });
  },
  openExternal(url) {
    return ipcRenderer.invoke('desktop:open-external', url);
  },
  getLicense() {
    return ipcRenderer.invoke('desktop:license');
  },
  submitFeedback(feedback) {
    return ipcRenderer.invoke('support:feedback', feedback);
  },
  quit() {
    return ipcRenderer.invoke('desktop:quit');
  },
  getUpdateStatus() {
    return ipcRenderer.invoke('updates:status');
  },
  checkForUpdates() {
    return ipcRenderer.invoke('updates:check');
  },
  downloadUpdate() {
    return ipcRenderer.invoke('updates:download');
  },
  applyUpdate() {
    return ipcRenderer.invoke('updates:apply');
  },
  getAppInfo() {
    return ipcRenderer.invoke('desktop:app-info');
  }
}));
