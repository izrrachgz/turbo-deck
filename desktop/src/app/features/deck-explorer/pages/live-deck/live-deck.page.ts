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

type SwipeAxis = 'pending' | 'horizontal' | 'vertical';

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
  readonly swipeOffset = signal(0);
  readonly swiping = signal(false);
  readonly deckTransform = computed(() => `translate3d(${this.swipeOffset()}px, 0, 0)`);
  readonly deckOpacity = computed(() => Math.max(.72, 1 - Math.abs(this.swipeOffset()) / 900));
  readonly slots = computed<DeckCellSlot[]>(() => {
    const byPosition = new Map(this.grid()?.cells.map(cell => [`${cell.rowIndex}:${cell.columnIndex}`, cell]) ?? []);
    return Array.from({ length: 24 }, (_, index) => {
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
  private swipePointerId: number | null = null;
  private swipeStartX = 0;
  private swipeStartY = 0;
  private swipeStartedAt = 0;
  private swipeWidth = 1;
  private swipeAxis: SwipeAxis = 'pending';
  private swipeDeltaX = 0;
  private suppressPointerClicksUntil = 0;

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

  triggerCell(cell: CellConfigModel | null, event: MouseEvent): void {
    if (event.detail > 0 && performance.now() < this.suppressPointerClicksUntil) {
      event.preventDefault();
      event.stopPropagation();
      return;
    }
    void this.trigger(cell);
  }

  onDeckPointerDown(event: PointerEvent): void {
    if (!event.isPrimary || event.button !== 0 || this.pages().length < 2 || this.triggeringCellId() !== null) return;
    this.swipePointerId = event.pointerId;
    this.swipeStartX = event.clientX;
    this.swipeStartY = event.clientY;
    this.swipeStartedAt = performance.now();
    this.swipeWidth = Math.max(1, (event.currentTarget as HTMLElement).clientWidth);
    this.swipeAxis = 'pending';
    this.swipeDeltaX = 0;
  }

  onDeckPointerMove(event: PointerEvent): void {
    if (event.pointerId !== this.swipePointerId) return;
    const deltaX = event.clientX - this.swipeStartX;
    const deltaY = event.clientY - this.swipeStartY;

    if (this.swipeAxis === 'pending') {
      if (Math.max(Math.abs(deltaX), Math.abs(deltaY)) < 8) return;
      this.swipeAxis = Math.abs(deltaX) > Math.abs(deltaY) ? 'horizontal' : 'vertical';
      if (this.swipeAxis === 'vertical') {
        this.resetSwipeTracking();
        return;
      }
      this.swiping.set(true);
      (event.currentTarget as HTMLElement).setPointerCapture(event.pointerId);
    }

    if (this.swipeAxis !== 'horizontal') return;
    event.preventDefault();
    this.swipeDeltaX = deltaX;
    const direction = deltaX < 0 ? 1 : -1;
    const hasPageInDirection = this.pageInDirection(direction) !== null;
    const resistedDelta = hasPageInDirection ? deltaX : deltaX * .22;
    const maximumOffset = this.swipeWidth * .42;
    this.swipeOffset.set(Math.max(-maximumOffset, Math.min(maximumOffset, resistedDelta)));
  }

  onDeckPointerUp(event: PointerEvent): void {
    if (event.pointerId !== this.swipePointerId) return;
    const wasHorizontalSwipe = this.swipeAxis === 'horizontal';
    const elapsed = Math.max(1, performance.now() - this.swipeStartedAt);
    const velocity = Math.abs(this.swipeDeltaX) / elapsed;
    const threshold = Math.min(84, this.swipeWidth * .18);
    const shouldChangePage = Math.abs(this.swipeDeltaX) >= threshold
      || (Math.abs(this.swipeDeltaX) >= 28 && velocity >= .5);
    const direction = this.swipeDeltaX < 0 ? 1 : -1;
    const destination = wasHorizontalSwipe && shouldChangePage ? this.pageInDirection(direction) : null;

    if (wasHorizontalSwipe) this.suppressPointerClicksUntil = performance.now() + 400;
    this.releasePointer(event);
    this.resetSwipeTracking();
    if (destination) void this.selectPage(destination.id);
  }

  onDeckPointerCancel(event: PointerEvent): void {
    if (event.pointerId !== this.swipePointerId) return;
    if (this.swipeAxis === 'horizontal') this.suppressPointerClicksUntil = performance.now() + 400;
    this.releasePointer(event);
    this.resetSwipeTracking();
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

  private pageInDirection(direction: number): DeckPageModel | null {
    const orderedPages = [...this.pages()].sort((left, right) => left.position - right.position);
    const currentIndex = orderedPages.findIndex(page => page.id === this.selectedPageId);
    if (currentIndex < 0) return null;
    return orderedPages[currentIndex + direction] ?? null;
  }

  private releasePointer(event: PointerEvent): void {
    const target = event.currentTarget as HTMLElement;
    if (target.hasPointerCapture(event.pointerId)) target.releasePointerCapture(event.pointerId);
  }

  private resetSwipeTracking(): void {
    this.swipePointerId = null;
    this.swipeAxis = 'pending';
    this.swipeDeltaX = 0;
    this.swiping.set(false);
    this.swipeOffset.set(0);
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
