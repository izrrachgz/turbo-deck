import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { PlaybackQueueItemModel } from '../../../../core/backend-api.service';
import { VisualizerPage } from './visualizer.page';

describe('VisualizerPage playback gating', () => {
  const item = (kind: 'audio' | 'image' | 'video'): PlaybackQueueItemModel => ({
    id: `${kind}-item`,
    cellConfigId: 1,
    mediaAssetId: 2,
    label: kind,
    kind,
    volume: 1,
    playbackRate: 1,
    pitchSemitones: 0,
    displayDurationMilliseconds: 5000,
    displayPermanently: false,
    displaySize: 'medium',
    animationInType: 'none',
    animationInDurationMilliseconds: 0,
    animationOutType: 'none',
    animationOutDurationMilliseconds: 0,
    priority: 'normal',
    queuedUtc: new Date().toISOString(),
  });

  it('does not hold images behind audio playback permission', async () => {
    TestBed.configureTestingModule({});
    const page = TestBed.runInInjectionContext(() => new VisualizerPage());
    const internal = page as unknown as {
      play(value: PlaybackQueueItemModel): Promise<void>;
      playImage(value: PlaybackQueueItemModel, mediaUrl: string): void;
    };
    const playImage = vi.spyOn(internal, 'playImage').mockImplementation(() => undefined);

    await internal.play(item('image'));

    expect(playImage).toHaveBeenCalledOnce();
    expect(page.playbackActivationRequired()).toBe(false);
    page.ngOnDestroy();
  });

  it('holds audio until one unlock and then retries the pending item', async () => {
    TestBed.configureTestingModule({});
    const page = TestBed.runInInjectionContext(() => new VisualizerPage());
    const internal = page as unknown as {
      play(value: PlaybackQueueItemModel): Promise<void>;
      markPlaybackEnabled(): Promise<void>;
    };
    const audio = item('audio');

    await internal.play(audio);
    expect(page.playbackActivationRequired()).toBe(true);

    const retry = vi.spyOn(internal, 'play').mockResolvedValue();
    await internal.markPlaybackEnabled();

    expect(retry).toHaveBeenCalledWith(audio);
    expect(page.playbackEnabled()).toBe(true);
    expect(page.playbackActivationRequired()).toBe(false);
    page.ngOnDestroy();
  });

  it('does not restart the current media when another item is added to pending', async () => {
    TestBed.configureTestingModule({});
    const page = TestBed.runInInjectionContext(() => new VisualizerPage());
    const current = item('audio');
    const internal = page as unknown as {
      current?: PlaybackQueueItemModel;
      play(value: PlaybackQueueItemModel): Promise<void>;
      reconcile(queue: { current: PlaybackQueueItemModel; pending: PlaybackQueueItemModel[] }): Promise<void>;
    };
    internal.current = current;
    const play = vi.spyOn(internal, 'play');

    await internal.reconcile({ current, pending: [item('video')] });

    expect(play).not.toHaveBeenCalled();
    page.ngOnDestroy();
  });

  it('normalizes decoded audio energy into a visibly varied waveform', () => {
    TestBed.configureTestingModule({});
    const page = TestBed.runInInjectionContext(() => new VisualizerPage());
    const samples = new Float32Array(800);
    samples.fill(0.01, 0, 200);
    samples.fill(0.08, 200, 400);
    samples.fill(0.25, 400, 600);
    samples.fill(0.8, 600);
    const internal = page as unknown as {
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
    page.ngOnDestroy();
  });
});
