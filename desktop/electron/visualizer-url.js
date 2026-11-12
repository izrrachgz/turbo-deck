'use strict';

function createVisualizerUrl(backendUrl) {
  if (!backendUrl) throw new Error('The backend is not ready.');
  return new URL('/visualizer', backendUrl).toString();
}

function createDeckExplorerUrl(backendUrl) {
  if (!backendUrl) throw new Error('The backend is not ready.');
  return new URL('/deck', backendUrl).toString();
}

function selectBrowserClientBaseUrl(controlUrl, advertisedUrls = [], preferredHostName) {
  const candidates = [];
  for (const value of advertisedUrls) {
    try {
      const candidate = new URL(value);
      if (candidate.protocol === 'http:' || candidate.protocol === 'https:') candidates.push(candidate);
    } catch {
      // Ignore malformed backend announcements and retain the private control endpoint.
    }
  }
  const secure = candidates.find(candidate => candidate.protocol === 'https:');
  const selected = secure ?? candidates[0];
  if (selected) {
    selected.hostname = preferredHostName || '127.0.0.1';
    return selected.toString();
  }
  return controlUrl;
}

module.exports = { createDeckExplorerUrl, createVisualizerUrl, selectBrowserClientBaseUrl };
