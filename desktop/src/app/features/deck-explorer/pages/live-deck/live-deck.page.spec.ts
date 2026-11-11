import { describe, expect, it } from 'vitest';

import type { CellConfigModel } from '../../../../core/backend-api.service';
import { LiveDeckPage } from './live-deck.page';

describe('LiveDeckPage', () => {
  const cell = (id: number, rowIndex: number, columnIndex: number): CellConfigModel => ({
    id,
    pageId: 10,
    rowIndex,
    columnIndex,
    label: `Cell ${id}`,
    enabled: true,
    mediaAssetId: 20 + id,
    thumbnailMediaAssetId: 30 + id,
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
    queuePriority: 'normal',
    createdUtc: new Date().toISOString(),
    modifiedUtc: new Date().toISOString(),
  });

  it('maps the 4 by 8 grid and reflects the backend-owned queue state', () => {
    const page = new LiveDeckPage();
    const first = cell(1, 0, 0);
    const last = cell(2, 3, 7);
    page.grid.set({
      page: {
        id: 10,
        profileId: 5,
        name: '1',
        position: 1,
        createdUtc: new Date().toISOString(),
        modifiedUtc: new Date().toISOString(),
      },
      cells: [last, first],
    });
    page.queue.set({
      current: {
        id: 'playing',
        cellConfigId: first.id,
        mediaAssetId: first.mediaAssetId!,
        label: first.label!,
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
    });

    expect(page.slots()).toHaveLength(32);
    expect(page.slots()[0].cell).toEqual(first);
    expect(page.slots()[31].cell).toEqual(last);
    expect(page.cellState(first)).toBe('current');
    expect(page.canTrigger(first)).toBe(true);
    expect(page.canTrigger({ ...first, enabled: false })).toBe(false);
  });
});
