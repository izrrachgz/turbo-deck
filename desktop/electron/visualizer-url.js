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
  const wildcardHosts = new Set(['0.0.0.0', '[::]', '::']);
  const loopbackHosts = new Set(['127.0.0.1', 'localhost', '[::1]', '::1']);
  const usable = candidates.filter(candidate => !wildcardHosts.has(candidate.hostname));
  const secure = usable.find(candidate => candidate.protocol === 'https:' && !loopbackHosts.has(candidate.hostname))
    ?? usable.find(candidate => candidate.protocol === 'https:');
  const network = usable.find(candidate => !loopbackHosts.has(candidate.hostname));
  const selected = secure ?? network ?? usable[0] ?? candidates[0];
  if (selected) {
    if (preferredHostName) selected.hostname = preferredHostName;
    else if (wildcardHosts.has(selected.hostname)) selected.hostname = '127.0.0.1';
    return selected.toString();
  }
  return controlUrl;
}

module.exports = { createDeckExplorerUrl, createVisualizerUrl, selectBrowserClientBaseUrl };
