'use strict';

const assert = require('node:assert/strict');
const test = require('node:test');
const { createDeckExplorerUrl, createVisualizerUrl } = require('./visualizer-url');

test('builds one fixed tokenless visualizer URL from the active backend origin', () => {
  assert.equal(createVisualizerUrl('http://127.0.0.1:52024/'), 'http://127.0.0.1:52024/visualizer');
});

test('builds one fixed tokenless live deck URL from the active backend origin', () => {
  assert.equal(createDeckExplorerUrl('http://192.168.1.24:52024/'), 'http://192.168.1.24:52024/deck');
});
