import { Component, OnInit, ViewChild, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DomSanitizer, SafeStyle } from '@angular/platform-browser';
import { LucideDynamicIcon } from '@lucide/angular';

import {
  BackendApiService,
  CellAnimationType,
  CellConfigModel,
  CellDisplaySize,
  DeckPageModel,
  MediaAssetModel,
  ProfileModel,
} from '../../../../../core/backend-api.service';
import { TextPipe } from '../../../../../core/i18n/text.pipe';
import { PreferencesService } from '../../../../../core/preferences/preferences.service';
import { ConfirmationDialogComponent } from '../../../../../shared/components/confirmations/confirmation-dialog/confirmation-dialog.component';
import { MediaCellComponent } from '../../../../../shared/components/media-cell/media-cell.component';
import { TextInputComponent } from '../../../../../shared/forms/text-input/text-input.component';
import { AnimationPlaygroundComponent } from '../../components/animation-playground/animation-playground.component';
import { CellMediaPreviewComponent } from '../../components/cell-media-preview/cell-media-preview.component';

interface GridPosition {
  readonly rowIndex: number;
  readonly columnIndex: number;
}

@Component({
  selector: 'td-deck-page',
  standalone: true,
  imports: [
    AnimationPlaygroundComponent,
    ConfirmationDialogComponent,
    CellMediaPreviewComponent,
    LucideDynamicIcon,
    MediaCellComponent,
    ReactiveFormsModule,
    TextInputComponent,
    TextPipe,
  ],
  templateUrl: './deck.page.html',
  styleUrl: './deck.page.css',
})
export class DeckPage implements OnInit {
  @ViewChild(CellMediaPreviewComponent) private cellPreview?: CellMediaPreviewComponent;

  private readonly api = inject(BackendApiService);
  private readonly fb = inject(FormBuilder);
  private readonly preferences = inject(PreferencesService);
  private readonly sanitizer = inject(DomSanitizer);
  private cellEditorSession = 0;

  readonly profiles = signal<readonly ProfileModel[]>([]);
  readonly pages = signal<readonly DeckPageModel[]>([]);
  readonly cells = signal<readonly CellConfigModel[]>([]);
  readonly mediaAssets = signal<readonly MediaAssetModel[]>([]);
  readonly loading = signal(true);
  readonly creatingProfile = signal(false);
  readonly loadingPage = signal(false);
  readonly savingCell = signal(false);
  readonly uploadingMedia = signal(false);
  readonly uploadingThumbnail = signal(false);
  readonly error = signal('');
  readonly profileCreationError = signal('');
  readonly notice = signal('');
  readonly mode = signal<'live' | 'edit'>('edit');
  readonly selectedProfileId = signal<number | null>(null);
  readonly selectedPageId = signal<number | null>(null);
  readonly profileDialogOpen = signal(false);
  readonly cellDialogOpen = signal(false);
  readonly selectedPosition = signal<GridPosition | null>(null);
  readonly positions: readonly GridPosition[] = Array.from({ length: 32 }, (_, index) => ({
    rowIndex: Math.floor(index / 8),
    columnIndex: index % 8,
  }));

  readonly profileForm = this.fb.nonNullable.group({ name: ['', [Validators.required, Validators.maxLength(100)]] });
  readonly cellForm = this.fb.nonNullable.group({
    label: ['', Validators.maxLength(80)],
    enabled: [true],
    mediaAssetId: [''],
    thumbnailMediaAssetId: [''],
    volume: [1, [Validators.required, Validators.min(0), Validators.max(1)]],
    playbackRate: [1, [Validators.required, Validators.min(0.25), Validators.max(4)]],
    pitchSemitones: [0, [Validators.required, Validators.min(-24), Validators.max(24)]],
    imageDuration: ['5000' as '3000' | '5000' | '10000' | 'permanent'],
    displaySize: ['medium' as CellDisplaySize],
    animationInType: ['none' as CellAnimationType],
    animationInDurationSeconds: [0, [Validators.required, Validators.min(0), Validators.max(10)]],
    animationOutType: ['none' as CellAnimationType],
    animationOutDurationSeconds: [0, [Validators.required, Validators.min(0), Validators.max(10)]],
    queueOverride: [false],
  });

  async ngOnInit(): Promise<void> {
    await Promise.all([this.loadProfiles(), this.loadMediaAssets()]);
  }

  enforceMinimumPlaybackRate(event: Event): void {
    const input = event.target as HTMLInputElement;
    const playbackRate = Math.max(0.25, Number(input.value));
    input.value = String(playbackRate);
    this.cellForm.controls.playbackRate.setValue(playbackRate);
  }

  cellAt(position: GridPosition): CellConfigModel | undefined {
    return this.cells().find((cell) => cell.rowIndex === position.rowIndex && cell.columnIndex === position.columnIndex);
  }

  async selectProfile(value: string): Promise<void> {
    const id = Number(value);
    this.selectedProfileId.set(Number.isFinite(id) ? id : null);
    await this.loadPages();
    await this.persistActiveProfile();
  }

  async selectPage(pageId: number): Promise<void> {
    if (pageId === this.selectedPageId() || this.loadingPage()) return;
    this.loadingPage.set(true);
    this.selectedPageId.set(pageId);
    try {
      await this.loadGrid();
    } finally {
      this.loadingPage.set(false);
    }
  }

  openProfileCreation(): void {
    this.profileForm.reset();
    this.profileCreationError.set('');
    this.profileDialogOpen.set(true);
  }

  async createProfile(): Promise<void> {
    if (this.profileForm.invalid) return this.profileForm.markAllAsTouched();
    this.creatingProfile.set(true);
    this.profileCreationError.set('');
    try {
      const created = await this.api.createProfile(this.profileForm.controls.name.value.trim());
      this.profiles.update((profiles) => [...profiles, created]);
      this.selectedProfileId.set(created.id);
      this.profileForm.reset();
      await this.loadPages();
      await this.persistActiveProfile();
      this.profileDialogOpen.set(false);
    } catch (error) {
      this.profileCreationError.set(this.message(error));
    } finally {
      this.creatingProfile.set(false);
    }
  }

  async activateCell(position: GridPosition): Promise<void> {
    if (this.mode() === 'live') {
      const liveCell = this.cellAt(position);
      if (!liveCell?.mediaAssetId) {
        this.notice.set('deck.mediaRequired');
        return;
      }
      this.error.set('');
      try {
        await this.api.queueCell(liveCell.id);
        this.notice.set('deck.queued');
      } catch (error) {
        this.error.set(this.message(error));
      }
      return;
    }
    const cell = this.cellAt(position);
    this.disposeCellEditor();
    this.selectedPosition.set(position);
    this.cellForm.reset({
      label: cell?.label ?? '',
      enabled: cell?.enabled ?? true,
      mediaAssetId: cell?.mediaAssetId?.toString() ?? '',
      thumbnailMediaAssetId: cell?.thumbnailMediaAssetId?.toString() ?? '',
      volume: cell?.volume ?? 1,
      playbackRate: cell?.playbackRate ?? 1,
      pitchSemitones: cell?.pitchSemitones ?? 0,
      imageDuration: cell?.displayPermanently
        ? 'permanent'
        : this.imageDurationValue(cell?.displayDurationMilliseconds),
      displaySize: cell?.displaySize ?? 'medium',
      animationInType: cell?.animationInType ?? 'none',
      animationInDurationSeconds: (cell?.animationInDurationMilliseconds ?? 0) / 1000,
      animationOutType: cell?.animationOutType ?? 'none',
      animationOutDurationSeconds: (cell?.animationOutDurationMilliseconds ?? 0) / 1000,
      queueOverride: cell?.queuePriority === 'override',
    });
    this.cellForm.markAsPristine();
    this.cellForm.markAsUntouched();
    this.error.set('');
    this.cellDialogOpen.set(true);
  }

  async saveCell(): Promise<void> {
    const pageId = this.selectedPageId();
    const position = this.selectedPosition();
    if (!pageId || !position || this.cellForm.invalid) return;
    this.savingCell.set(true);
    this.error.set('');
    try {
      const value = this.cellForm.getRawValue();
      const mediaKind = this.selectedMediaKind();
      const visualMedia = mediaKind === 'image' || mediaKind === 'video';
      const saved = await this.api.updateCell(pageId, position.rowIndex, position.columnIndex, {
        label: value.label.trim(),
        enabled: value.enabled,
        mediaAssetId: value.mediaAssetId ? Number(value.mediaAssetId) : null,
        thumbnailMediaAssetId: value.thumbnailMediaAssetId ? Number(value.thumbnailMediaAssetId) : null,
        volume: 1,
        playbackRate: mediaKind === 'audio' ? Number(value.playbackRate) : 1,
        pitchSemitones: mediaKind === 'audio' ? value.pitchSemitones : 0,
        displayDurationMilliseconds: mediaKind === 'image' && value.imageDuration !== 'permanent'
          ? Number(value.imageDuration)
          : 5000,
        displayPermanently: mediaKind === 'image' && value.imageDuration === 'permanent',
        displaySize: visualMedia ? value.displaySize : 'medium',
        animationInType: visualMedia ? value.animationInType : 'none',
        animationInDurationMilliseconds: visualMedia ? Math.round(value.animationInDurationSeconds * 1000) : 0,
        animationOutType: visualMedia ? value.animationOutType : 'none',
        animationOutDurationMilliseconds: visualMedia ? Math.round(value.animationOutDurationSeconds * 1000) : 0,
        queuePriority: value.queueOverride ? 'override' : 'normal',
      });
      this.cells.update((cells) => [...cells.filter((cell) => cell.id !== saved.id), saved]);
      this.setCellDialogOpen(false);
    } catch (error) {
      this.error.set(this.message(error));
    } finally {
      this.savingCell.set(false);
    }
  }

  async uploadMedia(): Promise<void> {
    const editorSession = this.cellEditorSession;
    this.uploadingMedia.set(true);
    this.error.set('');
    try {
      const created = await this.api.uploadMedia();
      if (!created) return;
      this.rememberMediaAsset(created);
      if (!this.cellDialogOpen() || editorSession !== this.cellEditorSession) return;
      this.cellForm.controls.mediaAssetId.setValue(created.id.toString());
    } catch (error) {
      this.error.set(this.message(error));
    } finally {
      this.uploadingMedia.set(false);
    }
  }

  async uploadThumbnail(): Promise<void> {
    const editorSession = this.cellEditorSession;
    this.uploadingThumbnail.set(true);
    this.error.set('');
    try {
      const created = await this.api.uploadMedia(undefined, 'image');
      if (!created) return;
      if (created.kind !== 'image') throw new Error('A cell background must use an image media asset.');
      this.rememberMediaAsset(created);
      if (!this.cellDialogOpen() || editorSession !== this.cellEditorSession) return;
      this.cellForm.controls.thumbnailMediaAssetId.setValue(created.id.toString());
    } catch (error) {
      this.error.set(this.message(error));
    } finally {
      this.uploadingThumbnail.set(false);
    }
  }

  selectedMediaKind(): MediaAssetModel['kind'] | null {
    const id = Number(this.cellForm.controls.mediaAssetId.value);
    return this.mediaAssets().find((asset) => asset.id === id)?.kind ?? null;
  }

  selectedMedia(): MediaAssetModel | undefined {
    const id = Number(this.cellForm.controls.mediaAssetId.value);
    return this.mediaAssets().find((asset) => asset.id === id);
  }

  selectedThumbnail(): MediaAssetModel | undefined {
    const id = Number(this.cellForm.controls.thumbnailMediaAssetId.value);
    return this.mediaAssets().find((asset) => asset.id === id && asset.kind === 'image');
  }

  selectedThumbnailBackground(): SafeStyle | null {
    const id = Number(this.cellForm.controls.thumbnailMediaAssetId.value);
    if (!Number.isSafeInteger(id) || id < 1) return null;
    return this.sanitizer.bypassSecurityTrustStyle(`url("turbo-deck-media://thumbnail/${id}")`);
  }

  previewDisplayDuration(): number {
    const duration = this.cellForm.controls.imageDuration.value;
    return duration === 'permanent' ? 5000 : Number(duration);
  }

  setCellDialogOpen(open: boolean): void {
    if (!open) this.disposeCellEditor();
    this.cellDialogOpen.set(open);
  }

  cellThumbnailUrl(position: GridPosition): string | null {
    const thumbnailId = this.cellAt(position)?.thumbnailMediaAssetId;
    return thumbnailId ? `turbo-deck-media://thumbnail/${thumbnailId}` : null;
  }

  private async loadProfiles(): Promise<void> {
    this.loading.set(true);
    this.error.set('');
    try {
      const profiles = await this.api.getProfiles();
      this.profiles.set(profiles);
      const preferredId = this.preferences.snapshot.activeProfileId;
      this.selectedProfileId.set(profiles.some((profile) => profile.id === preferredId) ? preferredId : (profiles[0]?.id ?? null));
      await this.loadPages();
    } catch (error) {
      this.error.set(this.message(error));
    } finally {
      this.loading.set(false);
    }
  }

  private async loadMediaAssets(): Promise<void> {
    try {
      this.mediaAssets.set(await this.api.getMediaAssets());
    } catch (error) {
      this.error.set(this.message(error));
    }
  }

  private async loadPages(): Promise<void> {
    const profileId = this.selectedProfileId();
    if (!profileId) {
      this.pages.set([]);
      this.selectedPageId.set(null);
      this.cells.set([]);
      return;
    }
    const pages = await this.api.getPages(profileId);
    this.pages.set(pages);
    this.selectedPageId.set(pages[0]?.id ?? null);
    await this.loadGrid();
  }

  private async loadGrid(): Promise<void> {
    const pageId = this.selectedPageId();
    if (!pageId) return this.cells.set([]);
    const grid = await this.api.getGrid(pageId);
    this.cells.set(grid.cells);
  }

  private message(error: unknown): string {
    return error instanceof Error ? error.message : String(error);
  }

  private async persistActiveProfile(): Promise<void> {
    await this.preferences.save({ ...this.preferences.snapshot, activeProfileId: this.selectedProfileId() });
  }

  private imageDurationValue(milliseconds?: number): '3000' | '5000' | '10000' {
    return milliseconds === 3000 || milliseconds === 10000 ? String(milliseconds) as '3000' | '10000' : '5000';
  }

  private disposeCellEditor(): void {
    this.cellPreview?.stop();
    this.cellEditorSession += 1;
    this.selectedPosition.set(null);
    this.cellForm.reset({
      label: '',
      enabled: true,
      mediaAssetId: '',
      thumbnailMediaAssetId: '',
      volume: 1,
      playbackRate: 1,
      pitchSemitones: 0,
      imageDuration: '5000',
      displaySize: 'medium',
      animationInType: 'none',
      animationInDurationSeconds: 0,
      animationOutType: 'none',
      animationOutDurationSeconds: 0,
      queueOverride: false,
    });
    this.cellForm.markAsPristine();
    this.cellForm.markAsUntouched();
  }

  private rememberMediaAsset(created: MediaAssetModel): void {
    this.mediaAssets.update((assets) => [created, ...assets.filter((asset) => asset.id !== created.id)]);
  }
}
