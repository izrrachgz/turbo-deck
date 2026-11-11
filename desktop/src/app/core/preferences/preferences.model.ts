import { ThemeMode, ThemePalette } from './theme-palettes';

export type SupportedCulture = 'en-US' | 'es-ES';

export interface AppPreferences {
  readonly activeProfileId: number | null;
  readonly culture: SupportedCulture;
  readonly theme: ThemeMode;
  readonly palette: ThemePalette;
  readonly timeZone: string;
  readonly queueCapacity: number;
  readonly visualizerSafeMargin: number;
}
