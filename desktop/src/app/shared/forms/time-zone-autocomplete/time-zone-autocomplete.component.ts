import { Component, forwardRef, inject, Input } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

import { I18nService } from '../../../core/i18n/i18n.service';
import { TextPipe } from '../../../core/i18n/text.pipe';
import { TimeZoneService } from '../../../core/time/time-zone.service';
import {
  AutocompleteSelectComponent,
  AutocompleteSelectOption,
  AutocompleteSelectValue,
} from '../autocomplete-select/autocomplete-select.component';

@Component({
  selector: 'td-time-zone-autocomplete',
  standalone: true,
  imports: [AutocompleteSelectComponent, TextPipe],
  templateUrl: './time-zone-autocomplete.component.html',
  providers: [{
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => TimeZoneAutocompleteComponent),
    multi: true,
  }],
})
export class TimeZoneAutocompleteComponent implements ControlValueAccessor {
  @Input() label = 'settings.timeZone';
  @Input() placeholder = 'settings.timeZonePlaceholder';
  @Input() maxLength = 64;

  private readonly i18n = inject(I18nService);
  private readonly timeZones = inject(TimeZoneService);
  private optionCulture = '';
  private cachedOptions: readonly AutocompleteSelectOption[] = [];
  private onChange: (value: string | null) => void = () => undefined;
  private onTouched: () => void = () => undefined;

  value = '';
  disabled = false;

  get options(): readonly AutocompleteSelectOption[] {
    if (this.optionCulture !== this.i18n.currentCulture) {
      this.optionCulture = this.i18n.currentCulture;
      this.cachedOptions = this.timeZones.options(this.optionCulture).map((option) => ({
        value: option.id,
        label: option.label,
        inputLabel: option.id,
      }));
    }
    return this.cachedOptions;
  }

  writeValue(value: string | null): void {
    this.value = value ?? '';
  }

  registerOnChange(fn: (value: string | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(disabled: boolean): void {
    this.disabled = disabled;
  }

  handleValueChange(value: AutocompleteSelectValue | null): void {
    if (value !== null && typeof value !== 'string') return;
    this.value = value ?? '';
    this.onChange(value);
  }

  handleTouched(): void {
    this.onTouched();
  }
}
