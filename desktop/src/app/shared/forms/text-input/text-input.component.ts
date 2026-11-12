import { Component, Input, Optional, Self, signal } from '@angular/core';
import { ControlValueAccessor, NgControl } from '@angular/forms';

@Component({
  selector: 'td-text-input',
  standalone: true,
  templateUrl: './text-input.component.html',
})
export class TextInputComponent implements ControlValueAccessor {
  @Input() label = '';
  @Input() placeholder = '';
  @Input() maxLength?: number;
  @Input() autocomplete = 'off';
  readonly inputId = `td-text-${crypto.randomUUID()}`;
  readonly value = signal('');
  readonly disabled = signal(false);

  private onChange: (value: string) => void = () => undefined;
  private onTouched: () => void = () => undefined;

  constructor(@Optional() @Self() public readonly ngControl: NgControl | null) {
    if (this.ngControl) this.ngControl.valueAccessor = this;
  }

  writeValue(value: string | null): void {
    this.value.set(value ?? '');
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(disabled: boolean): void {
    this.disabled.set(disabled);
  }

  handleInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.value.set(value);
    this.onChange(value);
  }

  markTouched(): void {
    this.onTouched();
  }
}
