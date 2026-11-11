import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { BackendApiService, CellConfigModel, TwitchRewardBindingModel, TwitchRewardModel } from '../../../../../core/backend-api.service';
import { TwitchPage } from './twitch.page';

describe('TwitchPage', () => {
  it('does not request rewards when status already requires reauthorization', async () => {
    const api = {
      getTwitchStatus: vi.fn().mockResolvedValue({
        configured: true,
        authorized: true,
        connected: false,
        lastError: 'Reconnect Twitch.',
      }),
      getTwitchRewards: vi.fn(),
    };
    TestBed.configureTestingModule({ providers: [{ provide: BackendApiService, useValue: api }] });
    const page = TestBed.runInInjectionContext(() => new TwitchPage());

    await page.refresh();

    expect(api.getTwitchRewards).not.toHaveBeenCalled();
    expect(page.status()?.lastError).toBe('Reconnect Twitch.');
  });

  it('replaces a cell reward binding from the grid selector', async () => {
    const oldReward = reward('old', 'Old reward');
    const newReward = reward('new', 'New reward');
    const existing = binding(oldReward, 10);
    const updated = binding(newReward, 10);
    const api = {
      saveTwitchBinding: vi.fn().mockResolvedValue(updated),
      deleteTwitchBinding: vi.fn().mockResolvedValue(undefined),
      getTwitchBindings: vi.fn().mockResolvedValue([updated]),
    };
    TestBed.configureTestingModule({ providers: [{ provide: BackendApiService, useValue: api }] });
    const page = TestBed.runInInjectionContext(() => new TwitchPage());
    page.rewards.set([oldReward, newReward]);
    page.bindings.set([existing]);
    page.cellSelections = { 10: oldReward.id };

    await page.changeBinding(cell(10), newReward.id);

    expect(api.saveTwitchBinding).toHaveBeenCalledWith(newReward, 10, true);
    expect(api.deleteTwitchBinding).toHaveBeenCalledWith(oldReward.id);
    expect(page.cellSelections[10]).toBe(newReward.id);
    expect(page.rewardOptions).toEqual([
      expect.objectContaining({ value: 'old', inputLabel: 'Old reward' }),
      expect.objectContaining({ value: 'new', inputLabel: 'New reward' }),
    ]);
  });

  it('does not expose empty or disabled cells as binding targets', () => {
    TestBed.configureTestingModule({ providers: [{ provide: BackendApiService, useValue: {} }] });
    const page = TestBed.runInInjectionContext(() => new TwitchPage());

    expect(page.canBind(undefined)).toBe(false);
    expect(page.canBind({ ...cell(1), enabled: false })).toBe(false);
    expect(page.canBind({ ...cell(1), mediaAssetId: null })).toBe(false);
    expect(page.canBind(cell(1))).toBe(true);

    page.cellSelections = { 1: 'reward-1' };
    page.openBindingDialog(cell(1));
    expect(page.bindingDialogOpen()).toBe(true);
    expect(page.selectedCell()?.id).toBe(1);
    expect(page.dialogRewardId()).toBe('reward-1');
  });

  it('does not restore a stale refresh error after disconnect succeeds', async () => {
    let rejectRewards: ((error: Error) => void) | undefined;
    const rewards = new Promise<never>((_, reject) => { rejectRewards = reject; });
    const api = {
      getTwitchStatus: vi.fn()
        .mockResolvedValueOnce({ configured: true, authorized: true, connected: false })
        .mockResolvedValueOnce({ configured: false, authorized: false, connected: false }),
      getTwitchRewards: vi.fn().mockReturnValue(rewards),
      getMediaAssets: vi.fn().mockResolvedValue([]),
      getTwitchBindings: vi.fn().mockResolvedValue([]),
      getProfiles: vi.fn().mockResolvedValue([]),
      disconnectTwitch: vi.fn().mockResolvedValue(undefined),
    };
    TestBed.configureTestingModule({ providers: [{ provide: BackendApiService, useValue: api }] });
    const page = TestBed.runInInjectionContext(() => new TwitchPage());

    const refresh = page.refresh();
    await vi.waitFor(() => expect(page.status()?.authorized).toBe(true));
    await page.disconnect();
    rejectRewards?.(new Error('Twitch authorization must be refreshed.'));
    await refresh;

    expect(page.status()?.authorized).toBe(false);
    expect(page.error()).toBe('');
    expect(page.notice()).toContain('tokens were purged');
  });
});

function reward(id: string, title: string): TwitchRewardModel {
  return { id, title, cost: 100, enabled: true };
}

function binding(value: TwitchRewardModel, cellConfigId: number): TwitchRewardBindingModel {
  return {
    id: value.id === 'old' ? 1 : 2,
    rewardId: value.id,
    rewardTitle: value.title,
    cellConfigId,
    enabled: true,
    createdUtc: new Date().toISOString(),
    modifiedUtc: new Date().toISOString(),
  };
}

function cell(id: number): CellConfigModel {
  return {
    id,
    pageId: 1,
    rowIndex: 0,
    columnIndex: 0,
    label: 'Media cell',
    enabled: true,
    mediaAssetId: 20,
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
  };
}
