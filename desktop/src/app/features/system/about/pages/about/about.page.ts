import { Component, ElementRef, OnInit, ViewChild, inject, signal } from '@angular/core';

import { BackendApiService, DesktopAppInfo } from '../../../../../core/backend-api.service';
import { TextPipe } from '../../../../../core/i18n/text.pipe';
import { ConfirmationDialogComponent } from '../../../../../shared/components/confirmations/confirmation-dialog/confirmation-dialog.component';

@Component({
  selector: 'td-about-page',
  standalone: true,
  imports: [ConfirmationDialogComponent, TextPipe],
  templateUrl: './about.page.html',
})
export class AboutPage implements OnInit {
  private readonly api = inject(BackendApiService);
  @ViewChild('licenseDialog') private licenseDialog?: ElementRef<HTMLDialogElement>;
  readonly info = signal<DesktopAppInfo | null>(null);
  readonly license = signal('');
  readonly error = signal('');
  readonly quitDialogOpen = signal(false);

  async ngOnInit(): Promise<void> {
    try {
      const [info, license] = await Promise.all([this.api.getAppInfo(), this.api.getLicense()]);
      this.info.set(info);
      this.license.set(license);
    } catch (error) {
      this.error.set(error instanceof Error ? error.message : String(error));
    }
  }

  showLicense(): void {
    this.licenseDialog?.nativeElement.showModal();
  }

  closeLicense(): void {
    this.licenseDialog?.nativeElement.close();
  }

  openRepository(): void {
    void this.api.openExternal('https://github.com/izrrachgz/turbo-deck');
  }

  quit(): void {
    void this.api.quitApplication();
  }
}
