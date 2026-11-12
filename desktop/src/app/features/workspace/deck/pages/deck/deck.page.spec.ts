import { FormBuilder } from '@angular/forms';
import { DomSanitizer } from '@angular/platform-browser';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { BackendApiService, CellConfigModel, MediaAssetModel } from '../../../../../core/backend-api.service';
import { PreferencesService } from '../../../../../core/preferences/preferences.service';
import { DeckPage } from './deck.page';

describe('DeckPage cell editor', () => {
  it('accepts audio playback rates through 4x and rejects higher values', () => {
    const page = createPage({});
    const playbackRate = page.cellForm.controls.playbackRate;

    playbackRate.setValue(4);
    expect(playbackRate.valid).toBe(true);

    playbackRate.setValue(4.25);
    expect(playbackRate.hasError('max')).toBe(true);
  });

  it('prevents the visual slider from selecting a rate below 0.25x', () => {
    const page = createPage({});
    const input = { value: '0' } as HTMLInputElement;

    page.enforceMinimumPlaybackRate({ target: input } as unknown as Event);

    expect(input.value).toBe('0.25');
    expect(page.cellForm.controls.playbackRate.value).toBe(0.25);
  });

  it('disposes edited values and loads every cell into a pristine form', async () => {
    const page = createPage({});
    page.cells.set([cell(1, 0, 0)]);

    await page.activateCell({ rowIndex: 0, columnIndex: 0 });

    expect(page.cellForm.controls.label.value).toBe('First cell');
    expect(page.cellForm.controls.mediaAssetId.value).toBe('21');
    expect(page.cellForm.pristine).toBe(true);
    page.cellForm.controls.label.setValue('Unsaved value');
    page.cellForm.controls.label.markAsDirty();

    page.setCellDialogOpen(false);

    expect(page.selectedPosition()).toBeNull();
    expect(page.cellForm.getRawValue()).toEqual(expect.objectContaining({
      label: '',
      mediaAssetId: '',
      thumbnailMediaAssetId: '',
      playbackRate: 1,
      pitchSemitones: 0,
    }));
    expect(page.cellForm.pristine).toBe(true);

    await page.activateCell({ rowIndex: 0, columnIndex: 1 });

    expect(page.cellForm.controls.label.value).toBe('');
    expect(page.cellForm.controls.mediaAssetId.value).toBe('');
    expect(page.cellForm.controls.thumbnailMediaAssetId.value).toBe('');
    expect(page.cellForm.pristine).toBe(true);
  });

  it('does not assign a late upload result to a different cell editor session', async () => {
    let completeUpload!: (asset: MediaAssetModel) => void;
    const upload = new Promise<MediaAssetModel>((resolve) => { completeUpload = resolve; });
    const page = createPage({ uploadMedia: vi.fn().mockReturnValue(upload) });
    page.cells.set([cell(1, 0, 0)]);
    await page.activateCell({ rowIndex: 0, columnIndex: 0 });

    const pendingUpload = page.uploadMedia();
    page.setCellDialogOpen(false);
    await page.activateCell({ rowIndex: 0, columnIndex: 1 });
    completeUpload(mediaAsset(90, 'Replacement audio', 'audio'));
    await pendingUpload;

    expect(page.cellForm.controls.mediaAssetId.value).toBe('');
    expect(page.mediaAssets().map((asset) => asset.id)).toContain(90);
  });

  it('exposes the assigned thumbnail for the modal preview', async () => {
    const page = createPage({});
    page.mediaAssets.set([mediaAsset(31, 'Cell artwork', 'image')]);
    page.cells.set([cell(1, 0, 0)]);

    await page.activateCell({ rowIndex: 0, columnIndex: 0 });

    expect(page.selectedThumbnail()?.displayName).toBe('Cell artwork');
    expect(page.selectedThumbnailBackground()).not.toBeNull();
  });
});

function createPage(api: object): DeckPage {
  TestBed.configureTestingModule({
    providers: [
      FormBuilder,
      { provide: BackendApiService, useValue: api },
      { provide: PreferencesService, useValue: { snapshot: { activeProfileId: null }, save: vi.fn() } },
      { provide: DomSanitizer, useValue: { bypassSecurityTrustStyle: (value: string) => value } },
    ],
  });
  return TestBed.runInInjectionContext(() => new DeckPage());
}

function cell(id: number, rowIndex: number, columnIndex: number): CellConfigModel {
  return {
    id,
    pageId: 1,
    rowIndex,
    columnIndex,
    label: 'First cell',
    enabled: true,
    mediaAssetId: 21,
    thumbnailMediaAssetId: 31,
    volume: 1,
    playbackRate: 1.5,
    pitchSemitones: 4,
    displayDurationMilliseconds: 5000,
    displayPermanently: false,
    displaySize: 'medium',
    animationInType: 'fade',
    animationInDurationMilliseconds: 1000,
    animationOutType: 'fade',
    animationOutDurationMilliseconds: 1000,
    queuePriority: 'normal',
    createdUtc: new Date().toISOString(),
    modifiedUtc: new Date().toISOString(),
  };
}

function mediaAsset(id: number, displayName: string, kind: MediaAssetModel['kind']): MediaAssetModel {
  return {
    id,
    displayName,
    kind,
    inspectionStatus: 'ready',
    hasAudio: kind === 'audio' || kind === 'video',
    file: {
      id,
      originalFileName: `${displayName}.bin`,
      contentType: 'application/octet-stream',
      extension: '.bin',
      size: 1,
      sha256: 'test',
      createdUtc: new Date().toISOString(),
      modifiedUtc: new Date().toISOString(),
    },
    createdUtc: new Date().toISOString(),
    modifiedUtc: new Date().toISOString(),
  };
}
