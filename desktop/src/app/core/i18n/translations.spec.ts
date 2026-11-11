import { describe, expect, it } from 'vitest';

import { TRANSLATIONS } from './translations';

describe('translations', () => {
  it('keeps English and Spanish catalogs in sync', () => {
    expect(Object.keys(TRANSLATIONS['es-ES']).sort()).toEqual(Object.keys(TRANSLATIONS['en-US']).sort());
  });
});
