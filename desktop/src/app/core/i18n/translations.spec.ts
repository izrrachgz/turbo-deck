import { describe, expect, it } from 'vitest';

import { TRANSLATIONS } from './translations';

describe('translations', () => {
  it('keeps English and Spanish catalogs in sync', () => {
    expect(Object.keys(TRANSLATIONS['es-ES']).sort()).toEqual(Object.keys(TRANSLATIONS['en-US']).sort());
  });

  it('provides localized network access guide titles', () => {
    expect(TRANSLATIONS['en-US']['network.accessGuideTitle']).toBe('Access Turbo Deck from another device');
    expect(TRANSLATIONS['es-ES']['network.accessGuideTitle']).toBe('Acceder a Turbo Deck desde otro dispositivo');
  });
});
