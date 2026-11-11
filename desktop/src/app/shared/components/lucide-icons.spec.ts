import { TestBed } from '@angular/core/testing';
import { LucideDynamicIcon, LucidePlay, provideLucideIcons } from '@lucide/angular';
import { describe, expect, it } from 'vitest';

describe('Lucide icon registration', () => {
  it('renders registered dynamic icons with SVG content', async () => {
    await TestBed.configureTestingModule({
      imports: [LucideDynamicIcon],
      providers: [provideLucideIcons(LucidePlay)],
    }).compileComponents();

    const fixture = TestBed.createComponent(LucideDynamicIcon);
    fixture.componentRef.setInput('lucideIcon', 'play');
    fixture.componentRef.setInput('size', 16);
    fixture.detectChanges();

    expect(fixture.nativeElement.getAttribute('width')).toBe('16');
    expect(fixture.nativeElement.querySelector('path')).not.toBeNull();
  });
});
