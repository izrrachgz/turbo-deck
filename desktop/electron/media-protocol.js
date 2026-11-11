'use strict';

const MEDIA_PROTOCOL_PRIVILEGES = Object.freeze({
  standard: true,
  secure: true,
  supportFetchAPI: true,
  corsEnabled: true,
  stream: true
});

function createMediaProtocolHandler(getBackendUrl, getControlToken, fetchImplementation = fetch) {
  return async request => {
    let target;
    try {
      target = new URL(request.url);
    } catch {
      return new Response(null, { status: 400 });
    }
    const match = /^\/([1-9]\d*)$/.exec(target.pathname);
    if (target.protocol !== 'turbo-deck-media:' || !['content', 'thumbnail'].includes(target.hostname) || !match) {
      return new Response(null, { status: 404 });
    }
    const backendUrl = getBackendUrl();
    if (!backendUrl) return new Response(null, { status: 503 });

    const headers = new Headers({ 'X-Turbo-Deck-Control-Token': getControlToken() });
    const range = request.headers?.get?.('Range');
    if (range) headers.set('Range', range);
    const response = await fetchImplementation(new URL(`/api/v1/media-assets/${match[1]}/content`, backendUrl), {
      headers,
      signal: AbortSignal.timeout(30_000)
    });
    if (!response.ok) return new Response(null, { status: response.status });
    const contentType = response.headers.get('Content-Type') ?? '';
    const mediaType = contentType.toLowerCase();
    const allowed = target.hostname === 'thumbnail'
      ? mediaType.startsWith('image/')
      : /^(?:audio|image|video)\//.test(mediaType);
    if (!allowed) return new Response(null, { status: 415 });
    const responseHeaders = new Headers({
      'Access-Control-Allow-Origin': '*',
      'Cache-Control': 'private, no-store',
      'Content-Type': contentType
    });
    for (const name of ['Accept-Ranges', 'Content-Length', 'Content-Range']) {
      const value = response.headers.get(name);
      if (value) responseHeaders.set(name, value);
    }
    return new Response(response.body, {
      status: response.status,
      headers: responseHeaders
    });
  };
}

module.exports = { createMediaProtocolHandler, MEDIA_PROTOCOL_PRIVILEGES };
