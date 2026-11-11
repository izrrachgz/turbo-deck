import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { LucideDynamicIcon } from '@lucide/angular';

import { BackendApiService, PlaybackQueueSnapshotModel } from '../../../../../core/backend-api.service';
import { TextPipe } from '../../../../../core/i18n/text.pipe';
import { ConfirmationDialogComponent } from '../../../../../shared/components/confirmations/confirmation-dialog/confirmation-dialog.component';

@Component({
  selector: 'td-queue-page',
  standalone: true,
  imports: [ConfirmationDialogComponent, LucideDynamicIcon, TextPipe],
  templateUrl: './queue.page.html',
})
export class QueuePage implements OnInit, OnDestroy {
  private readonly api = inject(BackendApiService);

  readonly queue = signal<PlaybackQueueSnapshotModel>({ current: null, pending: [] });
  readonly loading = signal(true);
  readonly processing = signal(false);
  readonly clearDialogOpen = signal(false);
  readonly error = signal('');
  private unsubscribeQueueChanged?: () => void;
  private refreshTimer?: ReturnType<typeof setInterval>;
  private refreshing = false;

  async ngOnInit(): Promise<void> {
    this.unsubscribeQueueChanged = this.api.onPlaybackQueueChanged(() => void this.refresh(false));
    await this.refresh();
    this.refreshTimer = setInterval(() => void this.refresh(false), 5000);
  }

  ngOnDestroy(): void {
    this.unsubscribeQueueChanged?.();
    if (this.refreshTimer) clearInterval(this.refreshTimer);
  }

  async refresh(showLoading = true): Promise<void> {
    if (this.refreshing) return;
    this.refreshing = true;
    if (showLoading) this.loading.set(true);
    this.error.set('');
    try {
      this.queue.set(await this.api.getPlaybackQueue());
    } catch (error) {
      this.error.set(this.message(error));
    } finally {
      this.loading.set(false);
      this.refreshing = false;
    }
  }

  async completeCurrent(): Promise<void> {
    const current = this.queue().current;
    if (!current) return;
    this.processing.set(true);
    try {
      this.queue.set(await this.api.completePlayback(current.id));
    } catch (error) {
      this.error.set(this.message(error));
    } finally {
      this.processing.set(false);
    }
  }

  async remove(itemId: string): Promise<void> {
    this.processing.set(true);
    this.error.set('');
    try {
      this.queue.set(await this.api.removePlaybackQueueItem(itemId));
    } catch (error) {
      this.error.set(this.message(error));
    } finally {
      this.processing.set(false);
    }
  }

  async move(itemId: string, offset: -1 | 1): Promise<void> {
    this.processing.set(true);
    this.error.set('');
    try {
      this.queue.set(await this.api.movePlaybackQueueItem(itemId, offset));
    } catch (error) {
      this.error.set(this.message(error));
    } finally {
      this.processing.set(false);
    }
  }

  async clear(): Promise<void> {
    this.processing.set(true);
    try {
      this.queue.set(await this.api.clearPlaybackQueue(true));
      this.clearDialogOpen.set(false);
    } catch (error) {
      this.error.set(this.message(error));
    } finally {
      this.processing.set(false);
    }
  }

  private message(error: unknown): string {
    return error instanceof Error ? error.message : String(error);
  }
}
