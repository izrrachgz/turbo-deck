import { describe, expect, it } from 'vitest';

import {
  isWildcardListenerAddress,
  listenerAddressErrors,
  listenerAddressKey,
  normalizeListenerAddresses,
  uniqueListenerAddresses,
} from './listener-address';

describe('listener addresses', () => {
  it('accepts listener aliases and IP addresses', () => {
    expect(listenerAddressErrors(['localhost', '192.168.1.20', '::1'])).toEqual([null, null, null]);
  });

  it('rejects hostnames and malformed addresses', () => {
    expect(listenerAddressErrors(['deck.local', '999.1.1.1', ''])).toEqual(['invalid', 'invalid', 'required']);
  });

  it('treats aliases for the same endpoint as duplicates', () => {
    expect(listenerAddressErrors(['localhost', '127.0.0.1'])).toEqual(['duplicate', 'duplicate']);
    expect(listenerAddressKey('*')).toBe(listenerAddressKey('0.0.0.0'));
  });

  it('rejects a wildcard mixed with specific listeners', () => {
    expect(listenerAddressErrors(['0.0.0.0', '192.168.1.20'])).toEqual(['wildcardConflict', 'wildcardConflict']);
  });

  it('trims and normalizes values before saving', () => {
    expect(normalizeListenerAddresses([' LOCALHOST ', ' 192.168.1.20 '])).toEqual(['localhost', '192.168.1.20']);
  });

  it('repairs repeated legacy listener aliases on load', () => {
    expect(uniqueListenerAddresses(['127.0.0.1', ' localhost ', '127.0.0.1'])).toEqual(['127.0.0.1']);
  });

  it('collapses a legacy wildcard and loopback combination to the wildcard', () => {
    expect(uniqueListenerAddresses(['127.0.0.1', '0.0.0.0'])).toEqual(['0.0.0.0']);
    expect(isWildcardListenerAddress('0.0.0.0')).toBe(true);
    expect(isWildcardListenerAddress('127.0.0.1')).toBe(false);
  });
});
