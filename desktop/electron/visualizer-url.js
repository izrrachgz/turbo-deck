'use strict';

function createVisualizerUrl(backendUrl) {
  if (!backendUrl) throw new Error('The backend is not ready.');
  return new URL('/visualizer', backendUrl).toString();
}

function createDeckExplorerUrl(backendUrl) {
  if (!backendUrl) throw new Error('The backend is not ready.');
  return new URL('/deck', backendUrl).toString();
}

module.exports = { createDeckExplorerUrl, createVisualizerUrl };
