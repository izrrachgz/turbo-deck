'use strict';

const assert = require('node:assert/strict');
const test = require('node:test');
const { assertTrustedSender } = require('./ipc-trust');

function createEvent(url = 'file:///C:/Turbo%20Deck/ui/index.html#/deck') {
  const frame = { url };
  const sender = { mainFrame: frame };
  return { event: { sender, senderFrame: frame }, sender };
}

test('allows the main frame of the trusted local renderer', () => {
  const { event, sender } = createEvent();
  assert.doesNotThrow(() => assertTrustedSender(event, sender));
});

test('rejects a different WebContents even when it has the same URL', () => {
  const { event } = createEvent();
  assert.throws(() => assertTrustedSender(event, { mainFrame: event.senderFrame }), /untrusted frame/);
});

test('rejects child frames and non-file renderers', () => {
  const { event, sender } = createEvent();
  event.senderFrame = { url: event.senderFrame.url };
  assert.throws(() => assertTrustedSender(event, sender), /untrusted frame/);

  const remote = createEvent('https://example.com/');
  assert.throws(() => assertTrustedSender(remote.event, remote.sender), /untrusted renderer/);
});
