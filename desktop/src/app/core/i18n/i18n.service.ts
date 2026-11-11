import { Injectable, inject } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

import { PreferencesService } from '../preferences/preferences.service';
import { SupportedCulture } from '../preferences/preferences.model';
import { TRANSLATIONS } from './translations';

@Injectable({ providedIn: 'root' })
export class I18nService {
  private readonly preferences = inject(PreferencesService);
  private readonly culture = new BehaviorSubject<SupportedCulture>(this.preferences.snapshot.culture);
  readonly culture$ = this.culture.asObservable();

  constructor() {
    this.preferences.preferences$.subscribe((preferences) => {
      if (preferences.culture !== this.culture.value) this.culture.next(preferences.culture);
    });
  }

  get currentCulture(): SupportedCulture {
    return this.culture.value;
  }

  text(key: string, params?: Readonly<Record<string, unknown>>): string {
    const value = TRANSLATIONS[this.currentCulture][key] ?? TRANSLATIONS['en-US'][key] ?? key;
    return value.replace(/\{\{\s*(\w+)\s*\}\}/g, (_, name: string) => String(params?.[name] ?? ''));
  }
}
