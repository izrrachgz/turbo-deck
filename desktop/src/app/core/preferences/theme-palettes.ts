export const ThemePalettes = [
  { id: 'midnight', mode: 'dark', labelKey: 'palette.midnight' },
  { id: 'graphite', mode: 'dark', labelKey: 'palette.graphite' },
  { id: 'ocean', mode: 'dark', labelKey: 'palette.ocean' },
  { id: 'violet', mode: 'dark', labelKey: 'palette.violet' },
  { id: 'ember', mode: 'dark', labelKey: 'palette.ember' },
  { id: 'daylight', mode: 'light', labelKey: 'palette.daylight' },
  { id: 'forest', mode: 'light', labelKey: 'palette.forest' },
  { id: 'rose', mode: 'light', labelKey: 'palette.rose' },
  { id: 'sunset', mode: 'light', labelKey: 'palette.sunset' },
  { id: 'nord', mode: 'light', labelKey: 'palette.nord' },
] as const;

export type ThemePalette = (typeof ThemePalettes)[number]['id'];
export type ThemeMode = (typeof ThemePalettes)[number]['mode'];
