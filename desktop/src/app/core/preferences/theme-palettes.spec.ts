import { describe, expect, it } from 'vitest';

import { ThemePalettes } from './theme-palettes';

describe('ThemePalettes', () => {
  it('provides five unique palettes for each mode', () => {
    expect(ThemePalettes.filter((palette) => palette.mode === 'dark')).toHaveLength(5);
    expect(ThemePalettes.filter((palette) => palette.mode === 'light')).toHaveLength(5);
    expect(new Set(ThemePalettes.map((palette) => palette.id)).size).toBe(ThemePalettes.length);
  });
});
