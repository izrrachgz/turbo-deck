import { CdkVirtualScrollViewport, ScrollingModule } from '@angular/cdk/scrolling';
import {
  Component,
  DestroyRef,
  ElementRef,
  EventEmitter,
  HostListener,
  Input,
  Optional,
  Output,
  Self,
  ViewChild,
} from '@angular/core';
import { ControlValueAccessor, NgControl } from '@angular/forms';
import { LucideDynamicIcon } from '@lucide/angular';

import { TextPipe } from '../../../core/i18n/text.pipe';

export type AutocompleteSelectValue = string | number;

export interface AutocompleteSelectOption {
  readonly value: AutocompleteSelectValue;
  readonly label: string;
  readonly icon?: string;
  readonly inputLabel?: string;
  readonly keywords?: readonly string[];
  readonly disabled?: boolean;
}

@Component({
  selector: 'td-autocomplete-select',
  standalone: true,
  host: { class: 'block min-w-0' },
  imports: [LucideDynamicIcon, ScrollingModule, TextPipe],
  templateUrl: './autocomplete-select.component.html',
  styleUrl: './autocomplete-select.component.css',
})
export class AutocompleteSelectComponent implements ControlValueAccessor {
  @Input() label = '';
  @Input() labelVisuallyHidden = false;
  @Input() placeholder = '';
  @Input() noResultsText = '';
  @Input() prefixIcon?: string;
  @Input() searchable = false;
  @Input() allowCustomValue = false;
  @Input() maxLength?: number;
  @Input() inputMode?: 'none' | 'text' | 'decimal' | 'numeric' | 'tel' | 'search' | 'email' | 'url';
  @Input() searchDebounceMs = 500;
  @Input() clearable = true;

  @Input()
  set options(value: readonly AutocompleteSelectOption[]) {
    const navigationChanged = !this.hasSameNavigationOptions(value);
    this.optionList = value;
    if (this.open) {
      if (!navigationChanged) return;
      this.activeIndex = this.firstEnabledIndex(this.filteredOptions);
      this.scrollToActiveOption();
    } else {
      this.syncDisplayText();
    }
  }
  get options(): readonly AutocompleteSelectOption[] {
    return this.optionList;
  }

  @Input()
  set value(value: AutocompleteSelectValue | null | undefined) {
    this.currentValue = value ?? null;
    if (!this.open) this.syncDisplayText();
  }
  get value(): AutocompleteSelectValue | null {
    return this.currentValue;
  }

  @Output() readonly valueChange = new EventEmitter<AutocompleteSelectValue | null>();
  @Output() readonly searchChange = new EventEmitter<string>();
  @Output() readonly touched = new EventEmitter<void>();

  @ViewChild('selectInput') private input?: ElementRef<HTMLInputElement>;
  @ViewChild(CdkVirtualScrollViewport) private viewport?: CdkVirtualScrollViewport;

  readonly inputId = `td-autocomplete-select-${crypto.randomUUID()}`;
  readonly labelId = `${this.inputId}-label`;
  readonly listId = `${this.inputId}-list`;
  readonly optionHeight = 48;
  readonly maxPanelHeight = 256;

  @Input() disabled = false;
  open = false;
  activeIndex = -1;
  searchQuery = '';
  displayText = '';

  private optionList: readonly AutocompleteSelectOption[] = [];
  private currentValue: AutocompleteSelectValue | null = null;
  private lastEmittedSearch?: string;
  private panelMouseDown = false;
  private searchTimer?: ReturnType<typeof setTimeout>;
  private onChange: (value: AutocompleteSelectValue | null) => void = () => undefined;
  private onTouched: () => void = () => undefined;

  constructor(
    @Optional() @Self() public readonly ngControl: NgControl | null,
    destroyRef: DestroyRef,
  ) {
    if (this.ngControl) this.ngControl.valueAccessor = this;
    destroyRef.onDestroy(() => this.cancelPendingSearch());
  }

  get filteredOptions(): readonly AutocompleteSelectOption[] {
    const query = this.searchQuery.trim().toLocaleLowerCase();
    if (!this.searchable || !query) return this.options;

    return this.options.filter((option) =>
      [option.label, option.inputLabel, String(option.value), ...(option.keywords ?? [])]
        .filter((value): value is string => !!value)
        .some((value) => value.toLocaleLowerCase().includes(query)),
    );
  }

  get activeDescendant(): string | null {
    return this.open && this.activeIndex >= 0 ? `${this.listId}-option-${this.activeIndex}` : null;
  }

  get viewportHeight(): number {
    return Math.min(this.maxPanelHeight, Math.max(1, this.filteredOptions.length) * this.optionHeight);
  }

  get hasClearableValue(): boolean {
    return (this.currentValue !== null && this.currentValue !== '') || this.searchQuery.length > 0;
  }

  writeValue(value: AutocompleteSelectValue | null): void {
    this.currentValue = value;
    this.lastEmittedSearch = undefined;
    this.syncDisplayText();
  }

  registerOnChange(fn: (value: AutocompleteSelectValue | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(disabled: boolean): void {
    this.disabled = disabled;
    if (disabled) this.closeList();
  }

  handleInput(event: Event): void {
    if (!this.searchable) return;
    const value = (event.target as HTMLInputElement).value;
    this.displayText = value;
    this.searchQuery = value;
    if (!this.allowCustomValue && this.currentValue !== null) this.updateValue(null);
    this.scheduleSearch(value);
    this.open = true;
    this.activeIndex = this.firstEnabledIndex(this.filteredOptions);
    if (this.allowCustomValue) this.updateValue(value);
    this.scrollToActiveOption();
  }

  handleFocus(): void {
    this.openList();
  }

  handleClick(): void {
    this.openList();
  }

  handleBlur(): void {
    if (this.panelMouseDown) return;
    this.onTouched();
    this.touched.emit();
    if (!this.allowCustomValue) this.syncDisplayText();
    this.closeList();
  }

  handlePanelMouseDown(event: MouseEvent): void {
    const target = event.target;
    if (event.button !== 0 || (target instanceof Element && target.closest('[role="option"]'))) return;
    this.panelMouseDown = true;
  }

  @HostListener('document:mouseup')
  handleDocumentMouseUp(): void {
    if (!this.panelMouseDown) return;
    this.panelMouseDown = false;
    if (this.open) this.input?.nativeElement.focus({ preventScroll: true });
  }

  handleKeydown(event: KeyboardEvent): void {
    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.open ? this.moveActiveOption(1) : this.openList();
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.open ? this.moveActiveOption(-1) : this.openList();
        break;
      case 'Home':
        if (this.open) {
          event.preventDefault();
          this.activeIndex = this.firstEnabledIndex(this.filteredOptions);
          this.scrollToActiveOption();
        }
        break;
      case 'End':
        if (this.open) {
          event.preventDefault();
          this.activeIndex = this.lastEnabledIndex(this.filteredOptions);
          this.scrollToActiveOption();
        }
        break;
      case 'Enter':
        if (this.open && this.activeIndex >= 0) {
          event.preventDefault();
          this.selectOption(this.filteredOptions[this.activeIndex]);
        }
        break;
      case 'Escape':
        if (this.open) {
          event.preventDefault();
          this.syncDisplayText();
          this.closeList();
        }
        break;
    }
  }

  toggleList(event: MouseEvent): void {
    event.preventDefault();
    if (this.disabled) return;
    if (this.open) {
      this.closeList();
    } else {
      this.input?.nativeElement.focus();
      this.openList();
    }
  }

  selectOption(option: AutocompleteSelectOption): void {
    if (option.disabled) return;
    this.cancelPendingSearch();
    this.currentValue = option.value;
    this.displayText = option.inputLabel ?? option.label;
    this.searchQuery = '';
    this.onChange(option.value);
    this.valueChange.emit(option.value);
    this.onTouched();
    this.touched.emit();
    this.input?.nativeElement.focus();
    this.closeList();
  }

  clearSelection(event?: Event): void {
    event?.preventDefault();
    event?.stopPropagation();
    if (this.disabled || !this.clearable || !this.hasClearableValue) return;
    this.cancelPendingSearch();
    this.lastEmittedSearch = undefined;
    this.currentValue = null;
    this.displayText = '';
    this.searchQuery = '';
    this.onChange(null);
    this.valueChange.emit(null);
    this.onTouched();
    this.touched.emit();
    this.input?.nativeElement.focus();
    this.closeList();
  }

  setActiveOption(index: number): void {
    if (!this.filteredOptions[index]?.disabled) this.activeIndex = index;
  }

  trackOption(_: number, option: AutocompleteSelectOption): AutocompleteSelectValue {
    return option.value;
  }

  isSelected(option: AutocompleteSelectOption): boolean {
    return option.value === this.currentValue;
  }

  private openList(): void {
    if (this.disabled || this.open) return;
    this.searchQuery = '';
    this.open = true;
    const selectedIndex = this.filteredOptions.findIndex((option) => this.isSelected(option));
    this.activeIndex = selectedIndex >= 0 ? selectedIndex : this.firstEnabledIndex(this.filteredOptions);
    this.scrollToActiveOption();
  }

  private closeList(): void {
    this.cancelPendingSearch();
    this.open = false;
    this.activeIndex = -1;
    this.searchQuery = '';
  }

  private moveActiveOption(direction: 1 | -1): void {
    const options = this.filteredOptions;
    if (!options.length) {
      this.activeIndex = -1;
      return;
    }
    let nextIndex = this.activeIndex;
    for (let attempts = 0; attempts < options.length; attempts += 1) {
      nextIndex = (nextIndex + direction + options.length) % options.length;
      if (!options[nextIndex].disabled) {
        this.activeIndex = nextIndex;
        this.scrollToActiveOption();
        return;
      }
    }
  }

  private firstEnabledIndex(options: readonly AutocompleteSelectOption[]): number {
    return options.findIndex((option) => !option.disabled);
  }

  private hasSameNavigationOptions(options: readonly AutocompleteSelectOption[]): boolean {
    return options.length === this.optionList.length && options.every((option, index) =>
      option.value === this.optionList[index].value && !!option.disabled === !!this.optionList[index].disabled,
    );
  }

  private lastEnabledIndex(options: readonly AutocompleteSelectOption[]): number {
    for (let index = options.length - 1; index >= 0; index -= 1) {
      if (!options[index].disabled) return index;
    }
    return -1;
  }

  private syncDisplayText(): void {
    const selected = this.options.find((option) => this.isSelected(option));
    this.displayText = selected?.inputLabel ?? selected?.label ?? String(this.currentValue ?? '');
  }

  private updateValue(value: AutocompleteSelectValue | null): void {
    this.currentValue = value;
    this.onChange(value);
    this.valueChange.emit(value);
  }

  private scheduleSearch(value: string): void {
    this.cancelPendingSearch();
    const normalized = value.trim();
    this.searchTimer = setTimeout(() => {
      this.searchTimer = undefined;
      if (normalized === this.lastEmittedSearch) return;
      this.lastEmittedSearch = normalized;
      this.searchChange.emit(normalized);
    }, Math.max(0, this.searchDebounceMs));
  }

  private cancelPendingSearch(): void {
    if (this.searchTimer === undefined) return;
    clearTimeout(this.searchTimer);
    this.searchTimer = undefined;
  }

  private scrollToActiveOption(): void {
    if (this.activeIndex < 0) return;
    requestAnimationFrame(() => this.viewport?.scrollToIndex(this.activeIndex));
  }
}
