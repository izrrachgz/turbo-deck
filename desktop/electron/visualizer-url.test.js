'use strict';

const assert = require('node:assert/strict');
const test = require('node:test');
const { createDeckExplorerUrl, createVisualizerUrl, selectBrowserClientBaseUrl } = require('./visualizer-url');

test('builds one fixed tokenless visualizer URL from the active backend origin', () => {
  assert.equal(createVisualizerUrl('http://127.0.0.1:52024/'), 'http://127.0.0.1:52024/visualizer');
});

test('builds one fixed tokenless live deck URL from the active backend origin', () => {
  assert.equal(createDeckExplorerUrl('http://192.168.1.24:52024/'), 'http://192.168.1.24:52024/deck');
});

test('prefers the trusted HTTPS listener over the private Electron control endpoint', () => {
  const selected = selectBrowserClientBaseUrl(
    'http://127.0.0.1:54079/',
    ['https://0.0.0.0:52024/', 'http://127.0.0.1:54079/']);

  assert.equal(selected, 'https://127.0.0.1:52024/');
  assert.equal(createVisualizerUrl(selected), 'https://127.0.0.1:52024/visualizer');
  assert.equal(createDeckExplorerUrl(selected), 'https://127.0.0.1:52024/deck');
});

test('prefers the advertised friendly hostname with the active HTTPS scheme', () => {
  assert.equal(
    selectBrowserClientBaseUrl(
      'http://127.0.0.1:54079/',
      ['https://0.0.0.0:52024/', 'http://127.0.0.1:54079/'],
      'turbo-deck.local'),
    'https://turbo-deck.local:52024/');
});

test('prefers the advertised friendly hostname with HTTP when HTTPS is disabled', () => {
  assert.equal(
    selectBrowserClientBaseUrl(
      'http://127.0.0.1:52024/',
      ['http://0.0.0.0:52024/'],
      'turbo-deck.local'),
    'http://turbo-deck.local:52024/');
});

test('retains HTTP when HTTPS is not configured', () => {
  assert.equal(
    selectBrowserClientBaseUrl('http://127.0.0.1:52024/', ['http://0.0.0.0:52024/']),
    'http://127.0.0.1:52024/');
});
