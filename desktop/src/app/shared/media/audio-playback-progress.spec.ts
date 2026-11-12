import { describe, expect, it } from 'vitest';

import { audioProgressSnapshot, formatAudioTime } from './audio-playback-progress';

describe('audio playback progress', () => {
  it('tracks source progress at the configured playback rate', () => {
    expect(audioProgressSnapshot(1000, 4000, 2)).toEqual({
      elapsedMilliseconds: 2000,
      percent: 50,
    });
    expect(audioProgressSnapshot(3000, 4000, 2)).toEqual({
      elapsedMilliseconds: 4000,
      percent: 100,
    });
  });

  it('formats elapsed audio time without advancing early', () => {
    expect(formatAudioTime(1999)).toBe('0:01');
    expect(formatAudioTime(4199, true)).toBe('0:05');
    expect(formatAudioTime(61000)).toBe('1:01');
  });
});
