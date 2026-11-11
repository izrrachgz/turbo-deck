import { Injectable } from '@angular/core';
import { tzName, tzOffset } from '@date-fns/tz';

export interface TimeZoneOption {
  readonly id: string;
  readonly label: string;
}

type IntlWithSupportedValues = typeof Intl & {
  supportedValuesOf?: (key: 'timeZone') => string[];
};

@Injectable({ providedIn: 'root' })
export class TimeZoneService {
  readonly browserTimeZone = resolveBrowserTimeZone();
  readonly timeZones = resolveTimeZones(this.browserTimeZone);

  options(locale: string): readonly TimeZoneOption[] {
    const now = new Date();

    return this.timeZones
      .map((id) => ({
        id,
        label: `${id} · ${safeTimeZoneName(id, now)} (${formatOffset(tzOffset(id, now))})`,
      }))
      .sort((left, right) => left.label.localeCompare(right.label, locale));
  }

  isValid(value: string | null | undefined): boolean {
    if (!value?.trim()) return false;

    try {
      const timeZone = value.trim();
      new Intl.DateTimeFormat('en-US', { timeZone }).format(new Date());
      return Number.isFinite(tzOffset(timeZone, new Date()));
    } catch {
      return false;
    }
  }
}

export const resolveBrowserTimeZone = (): string =>
  Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC';

const resolveTimeZones = (browserTimeZone: string): readonly string[] => {
  const supportedValuesOf = (Intl as IntlWithSupportedValues).supportedValuesOf;
  const supported = supportedValuesOf?.call(Intl, 'timeZone') ?? [];
  return [...new Set(['UTC', browserTimeZone, ...supported])].sort();
};

const safeTimeZoneName = (timeZone: string, date: Date): string => {
  try {
    return tzName(timeZone, date, 'longGeneric');
  } catch {
    return timeZone;
  }
};

const formatOffset = (minutes: number): string => {
  const sign = minutes < 0 ? '-' : '+';
  const absolute = Math.abs(minutes);
  const hours = Math.floor(absolute / 60).toString().padStart(2, '0');
  const remainder = (absolute % 60).toString().padStart(2, '0');
  return `UTC${sign}${hours}:${remainder}`;
};
