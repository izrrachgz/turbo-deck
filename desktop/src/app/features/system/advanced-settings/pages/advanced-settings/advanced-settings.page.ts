import { Component, OnInit, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { LucideDynamicIcon } from '@lucide/angular';

import {
  BackendApiService,
  DatabaseConnectionTestModel,
  DatabaseEngine,
  DatabaseEngineStateModel,
  DatabaseSettingsModel,
} from '../../../../../core/backend-api.service';
import { TextPipe } from '../../../../../core/i18n/text.pipe';
import { ConfirmationService } from '../../../../../core/routing/confirmation.service';

type ConnectionControls = Record<DatabaseEngine, FormControl<string>>;

@Component({
  selector: 'td-advanced-settings-page',
  standalone: true,
  imports: [LucideDynamicIcon, ReactiveFormsModule, TextPipe],
  templateUrl: './advanced-settings.page.html',
})
export class AdvancedSettingsPage implements OnInit {
  private readonly api = inject(BackendApiService);
  private readonly confirmations = inject(ConfirmationService);

  readonly loading = signal(true);
  readonly processingEngine = signal<DatabaseEngine | null>(null);
  readonly settings = signal<DatabaseSettingsModel | null>(null);
  readonly selectedEngine = signal<DatabaseEngine>('sqlite');
  readonly results = signal<Partial<Record<DatabaseEngine, DatabaseConnectionTestModel>>>({});
  readonly message = signal('');
  readonly error = signal('');
  readonly controls: ConnectionControls = {
    sqlite: new FormControl('', { nonNullable: true }),
    mssql: new FormControl('', { nonNullable: true }),
    pgsql: new FormControl('', { nonNullable: true }),
    mysql: new FormControl('', { nonNullable: true }),
  };

  ngOnInit(): void {
    void this.load();
  }

  async load(): Promise<void> {
    this.loading.set(true);
    this.error.set('');
    try {
      const settings = await this.api.getDatabaseSettings();
      this.settings.set(settings);
      this.selectedEngine.set(settings.activeEngine);
    } catch (error) {
      this.error.set(this.errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  selectEngine(value: string): void {
    this.selectedEngine.set(value as DatabaseEngine);
  }

  async testConnection(engine: DatabaseEngine): Promise<void> {
    if (this.processingEngine()) return;
    this.processingEngine.set(engine);
    this.message.set('');
    this.error.set('');
    try {
      const result = await this.api.testDatabaseConnection(engine, this.controls[engine].value);
      this.results.update((current) => ({ ...current, [engine]: result }));
      if (!result.success) this.error.set(result.message);
    } catch (error) {
      this.error.set(this.errorMessage(error));
    } finally {
      this.processingEngine.set(null);
    }
  }

  async confirmSwitch(engine: DatabaseEngine): Promise<void> {
    if (this.processingEngine()) return;
    const confirmed = await this.confirmations.confirm({
      titleKey: 'advanced.switchTitle',
      messageKey: 'advanced.switchMessage',
      confirmLabelKey: 'advanced.saveSwitch',
      destructive: false,
    });
    if (!confirmed) return;

    this.processingEngine.set(engine);
    this.message.set('');
    this.error.set('');
    try {
      const result = await this.api.switchDatabaseEngine(engine, this.controls[engine].value);
      this.message.set(result.message);
      this.controls[engine].reset('');
      await this.load();
    } catch (error) {
      this.error.set(this.errorMessage(error));
    } finally {
      this.processingEngine.set(null);
    }
  }

  displayedStatus(engine: DatabaseEngineStateModel): string {
    return this.results()[engine.engine]?.status ?? engine.status;
  }

  statusClass(engine: DatabaseEngineStateModel): string {
    return this.displayedStatus(engine) === 'Error'
      ? 'border-[var(--td-danger-border)] bg-[var(--td-danger-surface)] text-[var(--td-danger-text)]'
      : this.displayedStatus(engine) === 'Connected'
        ? 'border-[var(--td-success-border)] bg-[var(--td-success-surface)] text-[var(--td-success-text)]'
        : 'border-[var(--td-border)] bg-[var(--td-surface-muted)] text-[var(--td-text-secondary)]';
  }

  resultMessage(engine: DatabaseEngineStateModel): string | null {
    return this.results()[engine.engine]?.message ?? engine.lastError ?? null;
  }

  private errorMessage(error: unknown): string {
    return error instanceof Error ? error.message : 'The database operation failed.';
  }
}
