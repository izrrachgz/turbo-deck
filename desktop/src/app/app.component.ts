import { CommonModule } from '@angular/common';
import { ChangeDetectorRef, Component, OnInit, inject } from '@angular/core';
import {
  BackendApiService, DesktopAppInfo, NetworkSettings, StoredFile
} from './core/backend-api.service';

@Component({
  selector: 'td-root',
  standalone: true,
  imports: [CommonModule],
  template: `
    <header>
      <div class="brand-mark">TD</div>
      <div>
        <p class="eyebrow">DESKTOP APPLICATION SCAFFOLD</p>
        <h1>Turbo Deck</h1>
      </div>
      <span class="runtime" [class.ready]="ready">{{ ready ? 'BACKEND READY' : 'STARTING' }}</span>
    </header>

    <main>
      <section class="hero">
        <div>
          <p class="eyebrow">ANGULAR + ELECTRON + .NET 10</p>
          <h2>A clean deck for the next local-first app.</h2>
          <p class="lede">The renderer is sandboxed behind Electron IPC. SQLite, files, SignalR, LAN discovery,
            and the CQRS backend skeleton are ready for product modules.</p>
        </div>
        <button type="button" (click)="refresh()" [disabled]="loading">Refresh status</button>
      </section>

      <p class="error" *ngIf="error">{{ error }}</p>

      <div class="grid" *ngIf="!loading">
        <section class="card">
          <span class="card-index">01</span>
          <h3>Backend</h3>
          <dl>
            <div><dt>Status</dt><dd>{{ ready ? 'Ready' : 'Unavailable' }}</dd></div>
            <div><dt>Mode</dt><dd>{{ network?.mode === 1 ? 'LAN' : 'Loopback' }}</dd></div>
            <div><dt>Port</dt><dd>{{ network?.port === 0 ? 'Dynamic' : network?.port }}</dd></div>
            <div><dt>Discovery</dt><dd>{{ network?.discoveryEnabled ? 'Enabled' : 'Disabled' }}</dd></div>
          </dl>
        </section>

        <section class="card files-card">
          <span class="card-index">02</span>
          <div class="card-heading">
            <div><h3>Files</h3><p>{{ files.length }} stored item{{ files.length === 1 ? '' : 's' }}</p></div>
            <button type="button" (click)="upload()" [disabled]="uploading">
              {{ uploading ? 'Uploading…' : 'Upload file' }}
            </button>
          </div>
          <div class="empty" *ngIf="files.length === 0">No files yet. Upload one to verify the complete IPC/API/storage path.</div>
          <ul *ngIf="files.length > 0">
            <li *ngFor="let file of files">
              <div><strong>{{ file.originalFileName }}</strong><small>{{ formatBytes(file.size) }}</small></div>
              <button class="quiet" type="button" (click)="remove(file)">Remove</button>
            </li>
          </ul>
        </section>

        <section class="card">
          <span class="card-index">03</span>
          <h3>Realtime</h3>
          <p>The generic SignalR hub is available at <code>/hubs/realtime</code>. Add a narrow main-process adapter
            when the first product feature needs live events.</p>
          <span class="tag">CLIENTS</span><span class="tag">GROUPS</span><span class="tag">CQRS</span>
        </section>

        <section class="card">
          <span class="card-index">04</span>
          <h3>Runtime</h3>
          <dl>
            <div><dt>App</dt><dd>{{ appInfo?.version ?? 'development' }}</dd></div>
            <div><dt>Electron</dt><dd>{{ appInfo?.electronVersion ?? '—' }}</dd></div>
            <div><dt>Chromium</dt><dd>{{ appInfo?.chromiumVersion ?? '—' }}</dd></div>
            <div><dt>Node</dt><dd>{{ appInfo?.nodeVersion ?? '—' }}</dd></div>
          </dl>
        </section>
      </div>
    </main>
  `
})
export class AppComponent implements OnInit {
  private readonly api = inject(BackendApiService);
  private readonly changeDetector = inject(ChangeDetectorRef);
  loading = true;
  uploading = false;
  ready = false;
  error = '';
  files: StoredFile[] = [];
  network?: NetworkSettings;
  appInfo?: DesktopAppInfo;

  async ngOnInit(): Promise<void> {
    await this.refresh();
  }

  async refresh(): Promise<void> {
    this.loading = true;
    this.error = '';
    try {
      const [health, network, files, appInfo] = await Promise.all([
        this.api.getHealth(), this.api.getNetwork(), this.api.getFiles(), this.api.getAppInfo()
      ]);
      this.ready = health.status.toLowerCase() === 'ready';
      this.network = network;
      this.files = files;
      this.appInfo = appInfo;
    } catch (error) {
      this.ready = false;
      this.error = error instanceof Error ? error.message : String(error);
    } finally {
      this.loading = false;
      this.changeDetector.markForCheck();
    }
  }

  async upload(): Promise<void> {
    this.uploading = true;
    this.error = '';
    try {
      const created = await this.api.uploadFile('desktop');
      if (created) this.files = [created, ...this.files];
    } catch (error) {
      this.error = error instanceof Error ? error.message : String(error);
    } finally {
      this.uploading = false;
      this.changeDetector.markForCheck();
    }
  }

  async remove(file: StoredFile): Promise<void> {
    try {
      await this.api.deleteFile(file.id);
      this.files = this.files.filter(item => item.id !== file.id);
      this.changeDetector.markForCheck();
    } catch (error) {
      this.error = error instanceof Error ? error.message : String(error);
    }
  }

  formatBytes(size: number): string {
    if (size < 1024) return `${size} B`;
    if (size < 1024 * 1024) return `${(size / 1024).toFixed(1)} KB`;
    return `${(size / (1024 * 1024)).toFixed(1)} MB`;
  }
}
