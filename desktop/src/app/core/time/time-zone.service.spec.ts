import { TestBed } from '@angular/core/testing';

import { TimeZoneService } from './time-zone.service';

describe('TimeZoneService', () => {
  it('uses the browser timezone and exposes valid IANA values', () => {
    const service = TestBed.inject(TimeZoneService);

    expect(service.timeZones).toContain(service.browserTimeZone);
    expect(service.isValid('America/Hermosillo')).toBe(true);
    expect(service.isValid('Not/A_TimeZone')).toBe(false);
  });

  it('formats searchable labels with names and current UTC offsets', () => {
    const service = TestBed.inject(TimeZoneService);
    const hermosillo = service
      .options('en-US')
      .find((option) => option.id === 'America/Hermosillo');

    expect(hermosillo?.label).toContain('America/Hermosillo');
    expect(hermosillo?.label).toMatch(/UTC[-+]\d{2}:\d{2}/);
  });
});
