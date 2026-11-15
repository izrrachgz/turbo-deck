'use strict';

const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const test = require('node:test');

const desktopRoot = path.resolve(__dirname, '..');

test('packaged UI loads its stylesheet without an inline activation script', () => {
  const angularConfig = JSON.parse(
    fs.readFileSync(path.join(desktopRoot, 'angular.json'), 'utf8'),
  );
  const production = angularConfig.projects['turbo-deck-ui']
    .architect.build.configurations.production;

  assert.equal(production.optimization.styles.inlineCritical, false);
});

test('packaged UI security policy continues to reject inline scripts', () => {
  const indexHtml = fs.readFileSync(path.join(desktopRoot, 'src', 'index.html'), 'utf8');
  const policy = indexHtml.match(/Content-Security-Policy[^>]+content="([^"]+)"/)?.[1];

  assert.ok(policy, 'Expected a Content-Security-Policy meta tag');
  assert.match(policy, /script-src 'self'/);
  assert.doesNotMatch(policy, /script-src[^;]*'unsafe-inline'/);
});

test('standalone web pages use the Turbo Deck icon', () => {
  for (const filename of ['visualizer.html', 'deck.html']) {
    const html = fs.readFileSync(path.join(desktopRoot, 'src', filename), 'utf8');

    assert.match(
      html,
      /<link rel="icon" type="image\/png" href="assets\/turbo-deck\.png">/,
      `Expected ${filename} to reference the shared Turbo Deck icon`,
    );
  }
});
