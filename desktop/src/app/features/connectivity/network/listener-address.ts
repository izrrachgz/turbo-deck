export type ListenerAddressError = 'required' | 'invalid' | 'duplicate' | 'wildcardConflict' | null;

interface ParsedListenerAddress {
  normalized: string;
  key: string;
  wildcard: boolean;
}

export function listenerAddressErrors(addresses: readonly string[]): ListenerAddressError[] {
  const parsed = addresses.map(parseListenerAddress);
  const errors = parsed.map((address, index): ListenerAddressError => {
    if (!addresses[index].trim()) return 'required';
    return address ? null : 'invalid';
  });
  const counts = new Map<string, number>();

  for (const address of parsed) {
    if (address) counts.set(address.key, (counts.get(address.key) ?? 0) + 1);
  }
  for (let index = 0; index < parsed.length; index += 1) {
    const address = parsed[index];
    if (!errors[index] && address && (counts.get(address.key) ?? 0) > 1) errors[index] = 'duplicate';
  }

  if (parsed.length > 1 && parsed.some(address => address?.wildcard)) {
    for (let index = 0; index < parsed.length; index += 1) {
      if (!errors[index]) errors[index] = 'wildcardConflict';
    }
  }
  return errors;
}

export function normalizeListenerAddresses(addresses: readonly string[]): string[] {
  return addresses.map(address => parseListenerAddress(address)?.normalized ?? address.trim());
}

export function uniqueListenerAddresses(addresses: readonly string[]): string[] {
  const keys = new Set<string>();
  const unique = normalizeListenerAddresses(addresses).filter(address => {
    const key = listenerAddressKey(address) ?? `invalid:${address.toLowerCase()}`;
    if (keys.has(key)) return false;
    keys.add(key);
    return true;
  });
  const wildcard = unique.find(isWildcardListenerAddress);
  return wildcard ? [wildcard] : unique;
}

export function listenerAddressKey(address: string): string | null {
  return parseListenerAddress(address)?.key ?? null;
}

export function isWildcardListenerAddress(address: string): boolean {
  return parseListenerAddress(address)?.wildcard ?? false;
}

function parseListenerAddress(value: string): ParsedListenerAddress | null {
  const address = value.trim().toLowerCase();
  if (address === 'localhost' || address === '127.0.0.1') {
    return { normalized: address, key: 'ipv4:127.0.0.1', wildcard: false };
  }
  if (address === '*' || address === '+' || address === '0.0.0.0') {
    return { normalized: address, key: 'ipv4:0.0.0.0', wildcard: true };
  }
  if (isIpv4(address)) return { normalized: address, key: `ipv4:${address}`, wildcard: false };
  if (address === '::') return { normalized: address, key: 'ipv6:::0', wildcard: true };
  if (isIpv6(address)) return { normalized: address, key: `ipv6:${canonicalIpv6(address)}`, wildcard: false };
  return null;
}

function isIpv4(address: string): boolean {
  const parts = address.split('.');
  return parts.length === 4 && parts.every(part => /^\d{1,3}$/.test(part) && Number(part) <= 255);
}

function isIpv6(address: string): boolean {
  if (!address.includes(':') || !/^[0-9a-f:.]+$/i.test(address)) return false;
  try {
    return new URL(`http://[${address}]/`).hostname.startsWith('[');
  } catch {
    return false;
  }
}

function canonicalIpv6(address: string): string {
  return new URL(`http://[${address}]/`).hostname.slice(1, -1).toLowerCase();
}
