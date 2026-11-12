import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeStyle } from '@angular/platform-browser';
import { LucideDynamicIcon } from '@lucide/angular';

import {
  BackendApiService,
  CellConfigModel,
  DeckPageModel,
  MediaAssetModel,
  ProfileModel,
  TwitchConnectionStatusModel,
  TwitchDeviceAuthorizationModel,
  TwitchRewardBindingModel,
  TwitchRewardModel,
} from '../../../../../core/backend-api.service';
import { I18nService } from '../../../../../core/i18n/i18n.service';
import { TextPipe } from '../../../../../core/i18n/text.pipe';
import { ConfirmationDialogComponent } from '../../../../../shared/components/confirmations/confirmation-dialog/confirmation-dialog.component';
import {
  AutocompleteSelectComponent,
  AutocompleteSelectOption,
  AutocompleteSelectValue,
} from '../../../../../shared/forms/autocomplete-select/autocomplete-select.component';

interface GridPosition {
  readonly rowIndex: number;
  readonly columnIndex: number;
}

@Component({
  selector: 'td-twitch-page',
  standalone: true,
  imports: [AutocompleteSelectComponent, ConfirmationDialogComponent, FormsModule, LucideDynamicIcon, TextPipe],
  templateUrl: './twitch.page.html',
  styleUrl: './twitch.page.css',
})
export class TwitchPage implements OnInit, OnDestroy {
  private readonly api = inject(BackendApiService);
  private readonly i18n = inject(I18nService);
  private readonly sanitizer = inject(DomSanitizer);
  private pollTimer?: ReturnType<typeof setTimeout>;
  private rewardOptionSource?: readonly TwitchRewardModel[];
  private rewardOptionCulture = '';
  private cachedRewardOptions: readonly AutocompleteSelectOption[] = [];
  private connectionRevision = 0;

  readonly status = signal<TwitchConnectionStatusModel | null>(null);
  readonly authorization = signal<TwitchDeviceAuthorizationModel | null>(null);
  readonly rewards = signal<readonly TwitchRewardModel[]>([]);
  readonly bindings = signal<readonly TwitchRewardBindingModel[]>([]);
  readonly profiles = signal<readonly ProfileModel[]>([]);
  readonly pages = signal<readonly DeckPageModel[]>([]);
  readonly cells = signal<readonly CellConfigModel[]>([]);
  readonly mediaAssets = signal<readonly MediaAssetModel[]>([]);
  readonly selectedProfileId = signal<number | null>(null);
  readonly selectedPageId = signal<number | null>(null);
  readonly selectedCell = signal<CellConfigModel | null>(null);
  readonly bindingDialogOpen = signal(false);
  readonly dialogRewardId = signal('');
  readonly savingCellId = signal<number | null>(null);
  readonly error = signal('');
  readonly notice = signal('');
  readonly busy = signal(false);
  readonly disconnectDialogOpen = signal(false);
  readonly positions: readonly GridPosition[] = Array.from({ length: 32 }, (_, index) => ({
    rowIndex: Math.floor(index / 8),
    columnIndex: index % 8,
  }));

  clientId = '';
  cellSelections: Record<number, string> = {};

  get rewardOptions(): readonly AutocompleteSelectOption[] {
    const rewards = this.rewards();
    if (this.rewardOptionSource !== rewards || this.rewardOptionCulture !== this.i18n.currentCulture) {
      this.rewardOptionSource = rewards;
      this.rewardOptionCulture = this.i18n.currentCulture;
      const points = this.i18n.text('twitch.points');
      this.cachedRewardOptions = rewards.map((reward) => ({
        value: reward.id,
        label: `${reward.title} · ${reward.cost} ${points}`,
        inputLabel: reward.title,
        keywords: [reward.id, String(reward.cost), reward.prompt ?? ''],
        disabled: !reward.enabled,
      }));
    }
    return this.cachedRewardOptions;
  }

  async ngOnInit(): Promise<void> {
    await this.refresh();
  }

  ngOnDestroy(): void {
    if (this.pollTimer) clearTimeout(this.pollTimer);
  }

  async refresh(): Promise<void> {
    const revision = this.connectionRevision;
    this.error.set('');
    try {
      const status = await this.api.getTwitchStatus();
      this.status.set(status);
      if (status.authorized && !status.lastError) await this.loadConnectedData();
    } catch (error) {
      if (revision === this.connectionRevision) this.error.set(this.errorText(error));
    }
  }

  async connect(): Promise<void> {
    this.busy.set(true);
    this.error.set('');
    try {
      const authorization = await this.api.startTwitchAuthorization(this.clientId);
      this.authorization.set(authorization);
      this.status.set(await this.api.getTwitchStatus());
      await this.api.openExternal(authorization.verificationUri);
      this.schedulePoll(authorization.pollIntervalSeconds);
    } catch (error) {
      this.error.set(this.errorText(error));
    } finally {
      this.busy.set(false);
    }
  }

  async pollAuthorization(): Promise<void> {
    try {
      const result = await this.api.pollTwitchAuthorization();
      this.status.set(result.status);
      if (result.pending) {
        this.schedulePoll(this.authorization()?.pollIntervalSeconds ?? 5);
      } else {
        this.authorization.set(null);
        this.notice.set('Twitch authorization completed. EventSub is connecting.');
        await this.loadConnectedData();
      }
    } catch (error) {
      this.error.set(this.errorText(error));
    }
  }

  async disconnect(): Promise<void> {
    this.connectionRevision += 1;
    this.busy.set(true);
    this.error.set('');
    this.notice.set('');
    try {
      await this.api.disconnectTwitch();
      if (this.pollTimer) clearTimeout(this.pollTimer);
      this.disconnectDialogOpen.set(false);
      this.authorization.set(null);
      this.rewards.set([]);
      this.bindings.set([]);
      this.profiles.set([]);
      this.pages.set([]);
      this.cells.set([]);
      this.mediaAssets.set([]);
      this.cellSelections = {};
      this.status.set(await this.api.getTwitchStatus());
      this.notice.set('Twitch disconnected and all stored OAuth tokens were purged. Reward bindings were retained.');
    } catch (error) {
      this.error.set(this.errorText(error));
    } finally {
      this.busy.set(false);
    }
  }

  openActivation(url: string): void {
    void this.api.openExternal(url);
  }

  cellAt(position: GridPosition): CellConfigModel | undefined {
    return this.cells().find((cell) => cell.rowIndex === position.rowIndex && cell.columnIndex === position.columnIndex);
  }

  canBind(cell: CellConfigModel | undefined): cell is CellConfigModel {
    return Boolean(cell?.enabled && cell.mediaAssetId);
  }

  rewardForCell(cellId: number): TwitchRewardModel | undefined {
    const rewardId = this.cellSelections[cellId];
    return this.rewards().find((reward) => reward.id === rewardId);
  }

  mediaForCell(cell: CellConfigModel): MediaAssetModel | undefined {
    return this.mediaAssets().find((media) => media.id === cell.mediaAssetId);
  }

  selectedProfile(): ProfileModel | undefined {
    return this.profiles().find((profile) => profile.id === this.selectedProfileId());
  }

  selectedPage(): DeckPageModel | undefined {
    return this.pages().find((page) => page.id === this.selectedPageId());
  }

  selectedDialogReward(): TwitchRewardModel | undefined {
    const rewardId = this.dialogRewardId();
    return rewardId ? this.rewards().find((reward) => reward.id === rewardId) : undefined;
  }

  thumbnailBackground(cell: CellConfigModel | undefined): SafeStyle | null {
    const thumbnailId = cell?.thumbnailMediaAssetId;
    if (!thumbnailId || !Number.isSafeInteger(thumbnailId) || thumbnailId < 1) return null;
    return this.sanitizer.bypassSecurityTrustStyle(`url("turbo-deck-media://thumbnail/${thumbnailId}")`);
  }

  async selectProfile(value: string): Promise<void> {
    const profileId = Number(value);
    this.selectedProfileId.set(Number.isSafeInteger(profileId) ? profileId : null);
    await this.loadPages();
  }

  async selectPage(pageId: number): Promise<void> {
    if (pageId === this.selectedPageId()) return;
    this.selectedPageId.set(pageId);
    await this.loadGrid();
  }

  openBindingDialog(cell: CellConfigModel | undefined): void {
    if (!this.canBind(cell)) return;
    this.selectedCell.set(cell);
    this.dialogRewardId.set(this.cellSelections[cell.id] ?? '');
    this.bindingDialogOpen.set(true);
  }

  setBindingDialogOpen(open: boolean): void {
    if (this.savingCellId() !== null) return;
    this.bindingDialogOpen.set(open);
    if (!open) this.selectedCell.set(null);
  }

  setDialogReward(value: AutocompleteSelectValue | null): void {
    this.dialogRewardId.set(typeof value === 'string' ? value : '');
  }

  async saveSelectedBinding(): Promise<void> {
    const cell = this.selectedCell();
    if (!cell) return;
    if (await this.changeBinding(cell, this.dialogRewardId())) this.setBindingDialogOpen(false);
  }

  async changeBinding(cell: CellConfigModel, value: AutocompleteSelectValue | null): Promise<boolean> {
    if (!this.canBind(cell) || this.savingCellId() !== null) return false;
    const rewardId = typeof value === 'string' ? value : '';
    const previousSelection = this.cellSelections[cell.id] ?? '';
    const existing = this.bindings().filter((binding) => binding.cellConfigId === cell.id);
    if (previousSelection === rewardId && existing.length <= 1) return true;

    this.savingCellId.set(cell.id);
    this.error.set('');
    this.notice.set('');
    this.cellSelections = { ...this.cellSelections, [cell.id]: rewardId };
    try {
      if (rewardId) {
        const reward = this.rewards().find((candidate) => candidate.id === rewardId);
        if (!reward) throw new Error('The selected Twitch reward is no longer available.');
        await this.api.saveTwitchBinding(reward, cell.id, true);
      }

      const obsolete = existing.filter((binding) => binding.rewardId !== rewardId);
      await Promise.all(obsolete.map((binding) => this.api.deleteTwitchBinding(binding.rewardId)));
      await this.loadBindings();
      this.notice.set(rewardId ? 'Twitch reward mapping saved.' : 'Twitch reward mapping removed.');
      return true;
    } catch (error) {
      this.syncSelections();
      this.error.set(this.errorText(error));
      return false;
    } finally {
      this.savingCellId.set(null);
    }
  }

  private schedulePoll(seconds: number): void {
    if (this.pollTimer) clearTimeout(this.pollTimer);
    this.pollTimer = setTimeout(() => void this.pollAuthorization(), Math.max(1, seconds) * 1000);
  }

  private async loadConnectedData(): Promise<void> {
    const [rewards, mediaAssets] = await Promise.all([
      this.api.getTwitchRewards(),
      this.api.getMediaAssets(),
      this.loadBindings(),
      this.loadProfiles(),
    ]);
    this.rewards.set(rewards);
    this.mediaAssets.set(mediaAssets);
  }

  private async loadBindings(): Promise<void> {
    this.bindings.set(await this.api.getTwitchBindings());
    this.syncSelections();
  }

  private syncSelections(): void {
    const selections: Record<number, string> = {};
    for (const binding of this.bindings()) selections[binding.cellConfigId] = binding.rewardId;
    this.cellSelections = selections;
  }

  private async loadProfiles(): Promise<void> {
    const profiles = await this.api.getProfiles();
    this.profiles.set(profiles);
    const selected = profiles.find((profile) => profile.id === this.selectedProfileId()) ?? profiles[0];
    this.selectedProfileId.set(selected?.id ?? null);
    await this.loadPages();
  }

  private async loadPages(): Promise<void> {
    const profileId = this.selectedProfileId();
    if (!profileId) {
      this.pages.set([]);
      this.cells.set([]);
      this.selectedPageId.set(null);
      return;
    }

    const pages = await this.api.getPages(profileId);
    this.pages.set(pages);
    const profile = this.profiles().find((candidate) => candidate.id === profileId);
    const selected = pages.find((page) => page.id === this.selectedPageId())
      ?? pages.find((page) => page.position === profile?.selectedPageNumber)
      ?? pages[0];
    this.selectedPageId.set(selected?.id ?? null);
    await this.loadGrid();
  }

  private async loadGrid(): Promise<void> {
    const pageId = this.selectedPageId();
    this.cells.set(pageId ? (await this.api.getGrid(pageId)).cells : []);
  }

  private errorText(error: unknown): string {
    return error instanceof Error ? error.message : String(error);
  }
}
