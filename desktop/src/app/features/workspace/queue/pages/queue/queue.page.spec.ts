import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { BackendApiService, PlaybackQueueSnapshotModel } from '../../../../../core/backend-api.service';
import { QueuePage } from './queue.page';

describe('QueuePage', () => {
  it('refreshes when the Electron playback subscription reports a queue change', async () => {
    const populated: PlaybackQueueSnapshotModel = {
      current: {
        id: 'current',
        cellConfigId: 1,
        mediaAssetId: 2,
        label: 'Playing',
        kind: 'audio',
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
      },
      pending: [],
    };
    const empty: PlaybackQueueSnapshotModel = { current: null, pending: [] };
    let notifyQueueChanged: (() => void) | undefined;
    const unsubscribe = vi.fn();
    const api = {
      getPlaybackQueue: vi.fn()
        .mockResolvedValueOnce(populated)
        .mockResolvedValueOnce(empty),
      onPlaybackQueueChanged: vi.fn((callback: () => void) => {
        notifyQueueChanged = callback;
        return unsubscribe;
      }),
    };
    TestBed.configureTestingModule({ providers: [{ provide: BackendApiService, useValue: api }] });
    const page = TestBed.runInInjectionContext(() => new QueuePage());

    await page.ngOnInit();
    expect(page.queue().current?.id).toBe('current');
    notifyQueueChanged?.();
    await vi.waitFor(() => expect(page.queue().current).toBeNull());

    page.ngOnDestroy();
    expect(api.getPlaybackQueue).toHaveBeenCalledTimes(2);
    expect(unsubscribe).toHaveBeenCalledOnce();
  });

  it('applies backend snapshots after moving and removing queue items', async () => {
    const initial: PlaybackQueueSnapshotModel = {
      current: null,
      pending: [queueItem('first'), queueItem('second')],
    };
    const moved: PlaybackQueueSnapshotModel = {
      current: null,
      pending: [queueItem('second'), queueItem('first')],
    };
    const removed: PlaybackQueueSnapshotModel = {
      current: null,
      pending: [queueItem('first')],
    };
    const api = {
      movePlaybackQueueItem: vi.fn().mockResolvedValue(moved),
      removePlaybackQueueItem: vi.fn().mockResolvedValue(removed),
    };
    TestBed.configureTestingModule({ providers: [{ provide: BackendApiService, useValue: api }] });
    const page = TestBed.runInInjectionContext(() => new QueuePage());
    page.queue.set(initial);

    await page.move('second', -1);
    expect(api.movePlaybackQueueItem).toHaveBeenCalledWith('second', -1);
    expect(page.queue().pending.map(item => item.id)).toEqual(['second', 'first']);

    await page.remove('second');
    expect(api.removePlaybackQueueItem).toHaveBeenCalledWith('second');
    expect(page.queue().pending.map(item => item.id)).toEqual(['first']);
  });
});

function queueItem(id: string): NonNullable<PlaybackQueueSnapshotModel['current']> {
  return {
    id,
    cellConfigId: 1,
    mediaAssetId: 2,
    label: id,
    kind: 'audio',
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
  };
}
