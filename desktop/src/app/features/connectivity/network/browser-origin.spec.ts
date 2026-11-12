import { describe, expect, it } from 'vitest';

import { browserOriginErrors, normalizeBrowserOrigins, uniqueBrowserOrigins } from './browser-origin';

describe('browser origins', () => {
  it('accepts HTTP and HTTPS origins with optional ports', () => {
    expect(browserOriginErrors(['http://localhost:4200', 'https://192.168.1.20:8443'])).toEqual([null, null]);
  });

  it('rejects paths, credentials, unsupported schemes, and malformed values', () => {
    expect(browserOriginErrors([
      'https://example.com/app',
      'https://user:password@example.com',
      'ftp://example.com',
      'example.com',
    ])).toEqual(['invalid', 'invalid', 'invalid', 'invalid']);
  });

  it('normalizes trailing slashes and default ports', () => {
    expect(normalizeBrowserOrigins([' HTTPS://Example.COM:443/ ', 'http://localhost:80/'])).toEqual([
      'https://example.com',
      'http://localhost',
    ]);
  });

  it('detects equivalent duplicate origins', () => {
    expect(browserOriginErrors(['https://example.com', 'https://EXAMPLE.com:443/'])).toEqual(['duplicate', 'duplicate']);
  });

  it('repairs repeated legacy origins on load', () => {
    expect(uniqueBrowserOrigins(['https://example.com/', 'https://EXAMPLE.com:443', 'http://localhost:4200'])).toEqual([
      'https://example.com',
      'http://localhost:4200',
    ]);
  });
});
