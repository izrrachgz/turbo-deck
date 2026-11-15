import { describe, expect, it, vi } from 'vitest';

import type { CellConfigModel, DeckPageModel } from '../../../../core/backend-api.service';
import { LiveDeckPage } from './live-deck.page';

describe('LiveDeckPage', () => {
  const deckPage = (id: number, position: number): DeckPageModel => ({
    id,
    profileId: 5,
    name: String(position),
    position,
    createdUtc: new Date().toISOString(),
    modifiedUtc: new Date().toISOString(),
  });

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

  it('maps the 3 by 8 grid and reflects the backend-owned queue state', () => {
    const page = new LiveDeckPage();
    const first = cell(1, 0, 0);
    const last = cell(2, 2, 7);
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

    expect(page.slots()).toHaveLength(24);
    expect(page.slots()[0].cell).toEqual(first);
    expect(page.slots()[23].cell).toEqual(last);
    expect(page.cellState(first)).toBe('current');
    expect(page.canTrigger(first)).toBe(true);
    expect(page.canTrigger({ ...first, enabled: false })).toBe(false);
  });

  it('swipes between ordered pages and suppresses the cell click produced by the gesture', () => {
    const page = new LiveDeckPage();
    page.pages.set([deckPage(30, 3), deckPage(10, 1), deckPage(20, 2)]);
    page.selectedPageId = 20;
    const selectPage = vi.spyOn(page, 'selectPage').mockResolvedValue();
    const trigger = vi.spyOn(page, 'trigger').mockResolvedValue();
    const pointerTarget = {
      clientWidth: 400,
      setPointerCapture: vi.fn(),
      hasPointerCapture: vi.fn(() => true),
      releasePointerCapture: vi.fn(),
    };
    const pointerEvent = (clientX: number, clientY = 100): PointerEvent => ({
      pointerId: 7,
      isPrimary: true,
      button: 0,
      clientX,
      clientY,
      currentTarget: pointerTarget,
      preventDefault: vi.fn(),
    } as unknown as PointerEvent);

    page.onDeckPointerDown(pointerEvent(300));
    page.onDeckPointerMove(pointerEvent(190));
    page.onDeckPointerUp(pointerEvent(190));

    expect(selectPage).toHaveBeenCalledWith(30);
    expect(page.swipeOffset()).toBe(0);
    expect(pointerTarget.setPointerCapture).toHaveBeenCalledWith(7);
    expect(pointerTarget.releasePointerCapture).toHaveBeenCalledWith(7);

    const clickEvent = { detail: 1, preventDefault: vi.fn(), stopPropagation: vi.fn() } as unknown as MouseEvent;
    page.triggerCell(cell(1, 0, 0), clickEvent);
    expect(trigger).not.toHaveBeenCalled();
    expect(clickEvent.preventDefault).toHaveBeenCalled();
  });

  it('resists swiping past the first page without wrapping', () => {
    const page = new LiveDeckPage();
    page.pages.set([deckPage(10, 1), deckPage(20, 2)]);
    page.selectedPageId = 10;
    const selectPage = vi.spyOn(page, 'selectPage').mockResolvedValue();
    const pointerTarget = {
      clientWidth: 400,
      setPointerCapture: vi.fn(),
      hasPointerCapture: vi.fn(() => true),
      releasePointerCapture: vi.fn(),
    };
    const pointerEvent = (clientX: number): PointerEvent => ({
      pointerId: 8,
      isPrimary: true,
      button: 0,
      clientX,
      clientY: 100,
      currentTarget: pointerTarget,
      preventDefault: vi.fn(),
    } as unknown as PointerEvent);

    page.onDeckPointerDown(pointerEvent(100));
    page.onDeckPointerMove(pointerEvent(260));
    expect(page.swipeOffset()).toBeCloseTo(35.2);
    page.onDeckPointerUp(pointerEvent(260));

    expect(selectPage).not.toHaveBeenCalled();
    expect(page.swipeOffset()).toBe(0);
  });
});
