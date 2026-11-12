export type BrowserOriginError = 'required' | 'invalid' | 'duplicate' | null;

interface ParsedBrowserOrigin {
  normalized: string;
  key: string;
}

export function browserOriginErrors(origins: readonly string[]): BrowserOriginError[] {
  const parsed = origins.map(parseBrowserOrigin);
  const errors = parsed.map((origin, index): BrowserOriginError => {
    if (!origins[index].trim()) return 'required';
    return origin ? null : 'invalid';
  });
  const counts = new Map<string, number>();

  for (const origin of parsed) {
    if (origin) counts.set(origin.key, (counts.get(origin.key) ?? 0) + 1);
  }
  for (let index = 0; index < parsed.length; index += 1) {
    const origin = parsed[index];
    if (!errors[index] && origin && (counts.get(origin.key) ?? 0) > 1) errors[index] = 'duplicate';
  }
  return errors;
}

export function normalizeBrowserOrigins(origins: readonly string[]): string[] {
  return origins.map(origin => parseBrowserOrigin(origin)?.normalized ?? origin.trim());
}

export function uniqueBrowserOrigins(origins: readonly string[]): string[] {
  const keys = new Set<string>();
  return normalizeBrowserOrigins(origins).filter(origin => {
    const key = browserOriginKey(origin) ?? `invalid:${origin.toLowerCase()}`;
    if (keys.has(key)) return false;
    keys.add(key);
    return true;
  });
}

export function browserOriginKey(origin: string): string | null {
  return parseBrowserOrigin(origin)?.key ?? null;
}

function parseBrowserOrigin(value: string): ParsedBrowserOrigin | null {
  const candidate = value.trim();
  if (!candidate) return null;

  try {
    const url = new URL(candidate);
    if (!['http:', 'https:'].includes(url.protocol) || url.username || url.password || url.pathname !== '/' || url.search || url.hash) {
      return null;
    }
    return { normalized: url.origin, key: url.origin.toLowerCase() };
  } catch {
    return null;
  }
}
