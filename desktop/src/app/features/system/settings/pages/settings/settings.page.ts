import { Component, OnDestroy, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { LucideDynamicIcon } from '@lucide/angular';

import { TextPipe } from '../../../../../core/i18n/text.pipe';
import { AppPreferences } from '../../../../../core/preferences/preferences.model';
import { PreferencesService } from '../../../../../core/preferences/preferences.service';
import { ThemePalettes } from '../../../../../core/preferences/theme-palettes';
import { HasUnsavedChanges } from '../../../../../core/routing/unsaved-changes.guard';
import { TimeZoneService } from '../../../../../core/time/time-zone.service';
import {
  TimeZoneAutocompleteComponent,
} from '../../../../../shared/forms/time-zone-autocomplete/time-zone-autocomplete.component';

@Component({
  selector: 'td-settings-page',
  standalone: true,
  imports: [LucideDynamicIcon, ReactiveFormsModule, TextPipe, TimeZoneAutocompleteComponent],
  templateUrl: './settings.page.html',
  styleUrl: './settings.page.css',
})
export class SettingsPage implements HasUnsavedChanges, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly preferences = inject(PreferencesService);
  private readonly timeZones = inject(TimeZoneService);
  readonly saving = signal(false);
  readonly message = signal('');
  readonly error = signal('');
  readonly palettes = ThemePalettes;

  readonly form = this.fb.nonNullable.group({
    culture: [this.preferences.snapshot.culture, Validators.required],
    theme: [this.preferences.snapshot.theme, Validators.required],
    palette: [this.preferences.snapshot.palette, Validators.required],
    timeZone: [
      this.preferences.snapshot.timeZone,
      [Validators.required, Validators.maxLength(64), timeZoneValidator(this.timeZones)],
    ],
    queueCapacity: [this.preferences.snapshot.queueCapacity, [Validators.min(1), Validators.max(1000)]],
    visualizerSafeMargin: [this.preferences.snapshot.visualizerSafeMargin, [Validators.min(0), Validators.max(1000)]],
  });

  get availablePalettes() {
    return this.palettes.filter((palette) => palette.mode === this.form.controls.theme.value);
  }

  setTheme(theme: 'dark' | 'light'): void {
    this.form.controls.theme.setValue(theme);
    const current = this.palettes.find((palette) => palette.id === this.form.controls.palette.value);
    if (current?.mode !== theme) {
      this.form.controls.palette.setValue(theme === 'dark' ? 'midnight' : 'daylight');
    }
    this.form.markAsDirty();
    this.preview();
  }

  preview(): void {
    this.preferences.preview(this.formValue());
  }

  async save(): Promise<void> {
    if (this.form.invalid || this.saving()) return this.form.markAllAsTouched();
    this.saving.set(true);
    this.message.set('');
    this.error.set('');
    try {
      await this.preferences.save(this.formValue());
      this.form.markAsPristine();
      this.message.set('settings.saved');
    } catch {
      this.error.set('settings.saveFailed');
      this.preferences.restore();
    } finally {
      this.saving.set(false);
    }
  }

  ngOnDestroy(): void {
    if (this.form.dirty) this.preferences.restore();
  }

  hasUnsavedChanges(): boolean {
    return this.form.dirty && !this.saving();
  }

  private formValue(): AppPreferences {
    const raw = this.form.getRawValue();
    return {
      activeProfileId: this.preferences.snapshot.activeProfileId,
      culture: raw.culture,
      theme: raw.theme,
      palette: raw.palette,
      timeZone: raw.timeZone.trim(),
      queueCapacity: raw.queueCapacity,
      visualizerSafeMargin: raw.visualizerSafeMargin,
    };
  }
}

const timeZoneValidator = (timeZones: TimeZoneService): ValidatorFn =>
  (control: AbstractControl<string>): ValidationErrors | null =>
    !control.value || timeZones.isValid(control.value) ? null : { timeZone: true };
