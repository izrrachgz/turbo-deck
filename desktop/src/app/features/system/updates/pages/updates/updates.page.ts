import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';

import { BackendApiService, DesktopUpdateStatus } from '../../../../../core/backend-api.service';
import { TextPipe } from '../../../../../core/i18n/text.pipe';
import { ConfirmationDialogComponent } from '../../../../../shared/components/confirmations/confirmation-dialog/confirmation-dialog.component';

@Component({
  selector: 'td-updates-page',
  standalone: true,
  imports: [ConfirmationDialogComponent, TextPipe],
  templateUrl: './updates.page.html',
})
export class UpdatesPage implements OnInit, OnDestroy {
  private readonly api = inject(BackendApiService);
  private refreshTimer?: ReturnType<typeof setInterval>;
  readonly status = signal<DesktopUpdateStatus | null>(null);
  readonly error = signal('');
  readonly busy = signal(false);
  readonly applyDialogOpen = signal(false);

  async ngOnInit(): Promise<void> {
    await this.refresh();
    this.refreshTimer = setInterval(() => void this.refresh(), 750);
  }

  ngOnDestroy(): void {
    if (this.refreshTimer) clearInterval(this.refreshTimer);
  }

  async check(): Promise<void> {
    await this.run(() => this.api.checkForUpdates());
  }

  async download(): Promise<void> {
    await this.run(() => this.api.downloadUpdate());
  }

  async apply(): Promise<void> {
    this.busy.set(true);
    this.error.set('');
    try {
      await this.api.applyUpdate();
    } catch (error) {
      this.error.set(this.errorText(error));
      this.applyDialogOpen.set(false);
      this.busy.set(false);
    }
  }

  phaseKey(phase: DesktopUpdateStatus['phase']): string {
    return `updates.phase.${phase}`;
  }

  private async refresh(): Promise<void> {
    try {
      this.status.set(await this.api.getUpdateStatus());
    } catch (error) {
      this.error.set(this.errorText(error));
    }
  }

  private async run(operation: () => Promise<DesktopUpdateStatus>): Promise<void> {
    this.busy.set(true);
    this.error.set('');
    try {
      this.status.set(await operation());
    } catch (error) {
      this.error.set(this.errorText(error));
    } finally {
      this.busy.set(false);
    }
  }

  private errorText(error: unknown): string {
    return error instanceof Error ? error.message : String(error);
  }
}
