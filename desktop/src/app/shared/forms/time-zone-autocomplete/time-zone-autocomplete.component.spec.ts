import { TestBed } from '@angular/core/testing';
import { BehaviorSubject } from 'rxjs';
import {
  LucideCheck,
  LucideChevronDown,
  LucideClock,
  LucideX,
  provideLucideIcons,
} from '@lucide/angular';

import { I18nService } from '../../../core/i18n/i18n.service';
import { AutocompleteSelectComponent } from '../autocomplete-select/autocomplete-select.component';
import { TimeZoneAutocompleteComponent } from './time-zone-autocomplete.component';

describe('TimeZoneAutocompleteComponent', () => {
  beforeEach(async () => {
    const culture = new BehaviorSubject<'en-US' | 'es-ES'>('en-US');
    await TestBed.configureTestingModule({
      imports: [TimeZoneAutocompleteComponent],
      providers: [
        {
          provide: I18nService,
          useValue: {
            currentCulture: 'en-US',
            culture$: culture.asObservable(),
            text: (key: string) => key,
          },
        },
        provideLucideIcons(LucideCheck, LucideChevronDown, LucideClock, LucideX),
      ],
    }).compileComponents();
  });

  it('uses the backend-aligned maximum length and filters the virtualized options', () => {
    const fixture = TestBed.createComponent(TimeZoneAutocompleteComponent);
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;

    expect(input.maxLength).toBe(64);
    input.dispatchEvent(new FocusEvent('focus'));
    input.value = 'Hermosillo';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const autocomplete = fixture.debugElement.children[0].componentInstance as AutocompleteSelectComponent;
    expect(fixture.nativeElement.querySelector('cdk-virtual-scroll-viewport')).not.toBeNull();
    expect(autocomplete.filteredOptions.length).toBeGreaterThan(0);
    expect(
      autocomplete.filteredOptions.every((option) => option.label.includes('Hermosillo')),
    ).toBe(true);
  });
});
