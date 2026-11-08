'use strict';

const { contextBridge, ipcRenderer } = require('electron');

contextBridge.exposeInMainWorld('turboDeck', Object.freeze({
  request(method, path, body) {
    return ipcRenderer.invoke('backend:request', { method, path, body });
  },
  selectAndUploadFile(purpose) {
    return ipcRenderer.invoke('backend:upload-file', { purpose });
  },
  getAppInfo() {
    return ipcRenderer.invoke('desktop:app-info');
  }
}));
