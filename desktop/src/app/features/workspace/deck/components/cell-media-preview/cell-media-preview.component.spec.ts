import { describe, expect, it } from 'vitest';

import { CellMediaPreviewComponent } from './cell-media-preview.component';

describe('CellMediaPreviewComponent', () => {
  it('formats a string-backed playback rate without failing the audio preview', () => {
    const component = new CellMediaPreviewComponent();
    const internal = component as unknown as { formatRate(rate: number | string): string };

    expect(internal.formatRate('1.25')).toBe('1.25');
  });

  it('normalizes decoded audio energy into a visibly varied waveform', () => {
    const component = new CellMediaPreviewComponent();
    const samples = new Float32Array(800);
    samples.fill(0.01, 0, 200);
    samples.fill(0.08, 200, 400);
    samples.fill(0.25, 400, 600);
    samples.fill(0.8, 600);
    const internal = component as unknown as {
      waveformPeaks(serialized: string | undefined, player: unknown, bucketCount: number): number[];
    };

    const peaks = internal.waveformPeaks(undefined, {
      buffer: { getChannelData: () => samples },
    }, 4);

    expect(peaks).toHaveLength(4);
    expect(peaks[0]).toBeLessThan(peaks[1]);
    expect(peaks[1]).toBeLessThan(peaks[2]);
    expect(peaks[2]).toBeLessThan(peaks[3]);
    expect(peaks[3]).toBe(1);
  });
});
