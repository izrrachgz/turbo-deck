import { DOCUMENT } from '@angular/common';
import { inject, Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

import { AppSettingsModel, BackendApiService } from '../backend-api.service';
import { AppPreferences } from './preferences.model';
import { ThemePalettes } from './theme-palettes';

const STORAGE_KEY = 'turbo-deck.preferences';
const DEFAULT_PREFERENCES: AppPreferences = {
  activeProfileId: null,
  culture: 'en-US',
  theme: 'dark',
  palette: 'midnight',
  timeZone: browserTimeZone(),
  queueCapacity: 100,
  visualizerSafeMargin: 24,
};

@Injectable({ providedIn: 'root' })
export class PreferencesService {
  private readonly api = inject(BackendApiService);
  private readonly document = inject(DOCUMENT);
  private readonly state = new BehaviorSubject<AppPreferences>(this.readCache());
  readonly preferences$ = this.state.asObservable();

  constructor() {
    this.apply(this.snapshot);
  }

  get snapshot(): AppPreferences {
    return this.state.value;
  }

  async initialize(): Promise<void> {
    this.apply(this.snapshot);
    try {
      this.commit(this.fromModel(await this.api.getSettings()));
    } catch {
      // The local cache keeps the shell usable while the supervised backend restarts.
    }
  }

  preview(preferences: AppPreferences): void {
    this.apply(preferences);
  }

  restore(): void {
    this.apply(this.snapshot);
  }

  async save(preferences: AppPreferences): Promise<AppPreferences> {
    const saved = await this.api.updateSettings(preferences);
    const normalized = this.fromModel(saved);
    this.commit(normalized);
    return normalized;
  }

  private commit(preferences: AppPreferences): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(preferences));
    this.apply(preferences);
    this.state.next(preferences);
  }

  private readCache(): AppPreferences {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (!stored) return DEFAULT_PREFERENCES;
    try {
      return this.normalize(JSON.parse(stored) as Partial<AppPreferences>);
    } catch {
      return DEFAULT_PREFERENCES;
    }
  }

  private normalize(value: Partial<AppPreferences>): AppPreferences {
    const palette = ThemePalettes.find((candidate) => candidate.id === value.palette) ?? ThemePalettes[0];
    return {
      ...DEFAULT_PREFERENCES,
      ...value,
      theme: palette.mode,
      palette: palette.id,
      culture: value.culture === 'es-ES' ? 'es-ES' : 'en-US',
      timeZone: value.timeZone?.trim() || browserTimeZone(),
    };
  }

  private fromModel(model: AppSettingsModel): AppPreferences {
    return this.normalize({
      activeProfileId: model.activeProfileId ?? null,
      culture: model.culture,
      theme: model.theme,
      palette: model.palette as AppPreferences['palette'],
      timeZone: model.timeZone,
      queueCapacity: model.queueCapacity,
      visualizerSafeMargin: model.visualizerSafeMargin,
    });
  }

  private apply(preferences: AppPreferences): void {
    const root = this.document.documentElement;
    root.classList.add('td-themed');
    root.classList.toggle('theme-dark', preferences.theme === 'dark');
    root.classList.toggle('theme-light', preferences.theme === 'light');
    ThemePalettes.forEach((palette) =>
      root.classList.toggle(`theme-palette-${palette.id}`, palette.id === preferences.palette),
    );
    root.style.colorScheme = preferences.theme;
    root.lang = preferences.culture;
  }
}

function browserTimeZone(): string {
  return Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC';
}
