'use strict';

const assert = require('node:assert/strict');
const test = require('node:test');
const { createMediaProtocolHandler, MEDIA_PROTOCOL_PRIVILEGES } = require('./media-protocol');

test('content protocol permits cross-origin fetch for Tone audio previews', () => {
  assert.deepEqual(MEDIA_PROTOCOL_PRIVILEGES, {
    standard: true,
    secure: true,
    supportFetchAPI: true,
    corsEnabled: true,
    stream: true
  });
});

test('thumbnail protocol proxies only image assets with the private control token', async () => {
  let receivedUrl;
  let receivedToken;
  const handler = createMediaProtocolHandler(
    () => 'http://127.0.0.1:54321/',
    () => 'private-token',
    async (url, init) => {
      receivedUrl = url.toString();
      receivedToken = init.headers.get('X-Turbo-Deck-Control-Token');
      return new Response(new Uint8Array([1, 2, 3]), { headers: { 'Content-Type': 'image/png' } });
    }
  );

  const response = await handler({ url: 'turbo-deck-media://thumbnail/42' });

  assert.equal(response.status, 200);
  assert.equal(response.headers.get('Content-Type'), 'image/png');
  assert.equal(receivedUrl, 'http://127.0.0.1:54321/api/v1/media-assets/42/content');
  assert.equal(receivedToken, 'private-token');
});

test('thumbnail protocol rejects untrusted routes and non-image content', async () => {
  let fetchCount = 0;
  const handler = createMediaProtocolHandler(
    () => 'http://127.0.0.1:54321/',
    () => 'private-token',
    async () => {
      fetchCount += 1;
      return new Response('audio', { headers: { 'Content-Type': 'audio/mpeg' } });
    }
  );

  assert.equal((await handler({ url: 'turbo-deck-media://other/42' })).status, 404);
  assert.equal(fetchCount, 0);
  assert.equal((await handler({ url: 'turbo-deck-media://thumbnail/42' })).status, 415);
  assert.equal(fetchCount, 1);
});

test('content protocol proxies media range requests and preserves partial response headers', async () => {
  let receivedRange;
  const handler = createMediaProtocolHandler(
    () => 'http://127.0.0.1:54321/',
    () => 'private-token',
    async (_url, init) => {
      receivedRange = init.headers.get('Range');
      return new Response(new Uint8Array([1, 2, 3]), {
        status: 206,
        headers: {
          'Accept-Ranges': 'bytes',
          'Content-Range': 'bytes 0-2/10',
          'Content-Type': 'audio/mpeg'
        }
      });
    }
  );

  const response = await handler({
    url: 'turbo-deck-media://content/42',
    headers: new Headers({ Range: 'bytes=0-2' })
  });

  assert.equal(response.status, 206);
  assert.equal(response.headers.get('Access-Control-Allow-Origin'), '*');
  assert.equal(response.headers.get('Content-Range'), 'bytes 0-2/10');
  assert.equal(receivedRange, 'bytes=0-2');
});
