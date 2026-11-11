import { Component, OnDestroy, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

import type {
  CellConfigModel,
  DeckGridModel,
  DeckPageModel,
  PlaybackQueueSnapshotModel,
  ProfileModel,
} from '../../../../core/backend-api.service';

interface DeckCellSlot {
  rowIndex: number;
  columnIndex: number;
  cell: CellConfigModel | null;
}

@Component({
  selector: 'td-live-deck',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './live-deck.page.html',
  styleUrl: './live-deck.page.css',
})
export class LiveDeckPage implements OnInit, OnDestroy {
  readonly profiles = signal<ProfileModel[]>([]);
  readonly pages = signal<DeckPageModel[]>([]);
  readonly grid = signal<DeckGridModel | null>(null);
  readonly queue = signal<PlaybackQueueSnapshotModel>({ pending: [] });
  readonly loading = signal(true);
  readonly error = signal('');
  readonly connected = signal(false);
  readonly triggeringCellId = signal<number | null>(null);
  readonly slots = computed<DeckCellSlot[]>(() => {
    const byPosition = new Map(this.grid()?.cells.map(cell => [`${cell.rowIndex}:${cell.columnIndex}`, cell]) ?? []);
    return Array.from({ length: 32 }, (_, index) => {
      const rowIndex = Math.floor(index / 8);
      const columnIndex = index % 8;
      return { rowIndex, columnIndex, cell: byPosition.get(`${rowIndex}:${columnIndex}`) ?? null };
    });
  });

  selectedProfileId: number | null = null;
  selectedPageId: number | null = null;
  private connection?: HubConnection;
  private queuePoll?: ReturnType<typeof setInterval>;
  private gridPoll?: ReturnType<typeof setInterval>;
  private catalogPoll?: ReturnType<typeof setInterval>;
  private queueLoading = false;
  private gridLoading = false;

  async ngOnInit(): Promise<void> {
    await this.loadProfiles(true);
    await this.connectRealtime();
    this.queuePoll = setInterval(() => void this.loadQueue(), 1_000);
    this.gridPoll = setInterval(() => void this.loadGrid(), 5_000);
    this.catalogPoll = setInterval(() => void this.loadProfiles(), 10_000);
  }

  ngOnDestroy(): void {
    if (this.queuePoll) clearInterval(this.queuePoll);
    if (this.gridPoll) clearInterval(this.gridPoll);
    if (this.catalogPoll) clearInterval(this.catalogPoll);
    void this.connection?.stop();
  }

  async selectProfile(value: string | number | null): Promise<void> {
    const id = Number(value);
    if (!Number.isFinite(id) || id <= 0) return;
    this.selectedProfileId = id;
    await this.loadPages(id);
  }

  async selectPage(pageId: number): Promise<void> {
    this.selectedPageId = pageId;
    await this.loadGrid();
  }

  async trigger(cell: CellConfigModel | null): Promise<void> {
    if (!cell?.enabled || !cell.mediaAssetId || this.triggeringCellId() !== null) return;
    this.triggeringCellId.set(cell.id);
    this.error.set('');
    try {
      this.queue.set(await this.fetchJson<PlaybackQueueSnapshotModel>(
        `/api/v1/deck-explorer/cells/${cell.id}/trigger`,
        { method: 'POST' },
      ));
    } catch (error) {
      this.error.set(this.errorText(error));
    } finally {
      this.triggeringCellId.set(null);
    }
  }

  thumbnailUrl(cell: CellConfigModel): string | null {
    return cell.thumbnailMediaAssetId ? `/api/v1/deck-explorer/thumbnails/${cell.thumbnailMediaAssetId}` : null;
  }

  cellState(cell: CellConfigModel | null): 'current' | 'queued' | 'idle' {
    if (!cell) return 'idle';
    if (this.queue().current?.cellConfigId === cell.id) return 'current';
    return this.queue().pending.some(item => item.cellConfigId === cell.id) ? 'queued' : 'idle';
  }

  canTrigger(cell: CellConfigModel | null): boolean {
    return cell?.enabled === true && cell.mediaAssetId !== null && cell.mediaAssetId !== undefined;
  }

  private async loadProfiles(showLoading = false): Promise<void> {
    if (showLoading) this.loading.set(true);
    this.error.set('');
    try {
      const profiles = await this.fetchJson<ProfileModel[]>('/api/v1/deck-explorer/profiles');
      this.profiles.set(profiles);
      if (profiles.length) {
        const selected = profiles.find(profile => profile.id === this.selectedProfileId) ?? profiles[0];
        this.selectedProfileId = selected.id;
        await this.loadPages(selected.id);
      } else {
        this.pages.set([]);
        this.grid.set(null);
      }
      await this.loadQueue();
    } catch (error) {
      this.error.set(this.errorText(error));
    } finally {
      if (showLoading) this.loading.set(false);
    }
  }

  private async loadPages(profileId: number): Promise<void> {
    try {
      const pages = await this.fetchJson<DeckPageModel[]>(`/api/v1/deck-explorer/profiles/${profileId}/pages`);
      this.pages.set(pages);
      const selected = pages.find(page => page.id === this.selectedPageId) ??
        pages.find(page => page.position === this.profiles().find(profile => profile.id === profileId)?.selectedPageNumber) ??
        pages[0];
      this.selectedPageId = selected?.id ?? null;
      await this.loadGrid();
    } catch (error) {
      this.error.set(this.errorText(error));
    }
  }

  private async loadGrid(): Promise<void> {
    if (!this.selectedPageId || this.gridLoading) return;
    this.gridLoading = true;
    try {
      this.grid.set(await this.fetchJson<DeckGridModel>(`/api/v1/deck-explorer/pages/${this.selectedPageId}/grid`));
    } catch (error) {
      this.error.set(this.errorText(error));
    } finally {
      this.gridLoading = false;
    }
  }

  private async loadQueue(): Promise<void> {
    if (this.queueLoading) return;
    this.queueLoading = true;
    try {
      this.queue.set(await this.fetchJson<PlaybackQueueSnapshotModel>('/api/v1/deck-explorer/queue'));
    } catch (error) {
      this.error.set(this.errorText(error));
    } finally {
      this.queueLoading = false;
    }
  }

  private async connectRealtime(): Promise<void> {
    const clientId = `deck-${crypto.randomUUID()}`;
    this.connection = new HubConnectionBuilder()
      .withUrl(`/hubs/deck-explorer?clientId=${encodeURIComponent(clientId)}`)
      .withAutomaticReconnect([0, 1_000, 2_500, 5_000, 10_000])
      .configureLogging(LogLevel.Warning)
      .build();
    this.connection.on('Receive', () => void this.loadQueue());
    this.connection.onreconnecting(() => this.connected.set(false));
    this.connection.onreconnected(() => {
      this.connected.set(true);
      void this.loadQueue();
    });
    this.connection.onclose(() => this.connected.set(false));
    try {
      await this.connection.start();
      this.connected.set(true);
    } catch {
      this.connected.set(false);
    }
  }

  private async fetchJson<T>(path: string, init?: RequestInit): Promise<T> {
    const response = await fetch(path, {
      ...init,
      headers: { Accept: 'application/json', ...init?.headers },
      cache: 'no-store',
    });
    if (!response.ok) {
      const problem = await response.json().catch(() => null) as { title?: string } | null;
      throw new Error(problem?.title ?? `Request failed (${response.status}).`);
    }
    return response.json() as Promise<T>;
  }

  private errorText(error: unknown): string {
    return error instanceof Error ? error.message : String(error);
  }
}
