import { TestBed } from '@angular/core/testing';
import {
  LucideLayoutGrid,
  LucideMonitorSmartphone,
  LucidePencil,
  LucidePlus,
  LucideQrCode,
  LucideRadio,
  LucideRotateCw,
  LucideShield,
  LucideTrash2,
  provideLucideIcons,
} from '@lucide/angular';
import { BehaviorSubject } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import { BackendApiService, NetworkSettings } from '../../../../../core/backend-api.service';
import { I18nService } from '../../../../../core/i18n/i18n.service';
import { NetworkPage } from './network.page';

describe('NetworkPage', () => {
  it('publishes the asynchronously loaded discovery name through reactive state', async () => {
    const api = { getNetwork: vi.fn().mockResolvedValue(networkSettings()) };
    TestBed.configureTestingModule({ providers: [{ provide: BackendApiService, useValue: api }] });
    const page = TestBed.runInInjectionContext(() => new NetworkPage());

    await page.load();

    expect(page.instanceName()).toBe('Turbo Deck Backend');
  });

  it('replaces existing listeners when the wildcard suggestion is selected', () => {
    const api = { getNetwork: vi.fn().mockResolvedValue(networkSettings()) };
    TestBed.configureTestingModule({ providers: [{ provide: BackendApiService, useValue: api }] });
    const page = TestBed.runInInjectionContext(() => new NetworkPage());
    page.addressDraft = ['127.0.0.1'];

    page.addAddress('0.0.0.0');

    expect(page.addressDraft).toEqual(['0.0.0.0']);
  });

  it('validates and normalizes browser origins when the editor is saved', () => {
    const api = { getNetwork: vi.fn().mockResolvedValue(networkSettings()) };
    TestBed.configureTestingModule({ providers: [{ provide: BackendApiService, useValue: api }] });
    const page = TestBed.runInInjectionContext(() => new NetworkPage());
    page.corsAllowedOrigins = ['http://localhost:4200'];

    page.openCorsOriginDialog();
    page.addCorsOrigin();
    page.corsOriginDraft[1] = 'HTTPS://Example.COM:443/';

    expect(page.corsOriginEditorInvalid()).toBe(false);
    page.saveCorsOrigins();

    expect(page.corsAllowedOrigins).toEqual(['http://localhost:4200', 'https://example.com']);
    expect(page.corsOriginDialogOpen()).toBe(false);
  });

  it('opens the restart confirmation when Save and restart backend is selected', async () => {
    Object.defineProperty(HTMLDialogElement.prototype, 'showModal', {
      configurable: true,
      value(this: HTMLDialogElement) { this.setAttribute('open', ''); },
    });
    const api = { getNetwork: vi.fn().mockResolvedValue(networkSettings()) };
    const i18n = { culture$: new BehaviorSubject('en-US'), text: (key: string) => key };
    TestBed.configureTestingModule({
      imports: [NetworkPage],
      providers: [
        { provide: BackendApiService, useValue: api },
        { provide: I18nService, useValue: i18n },
        provideLucideIcons(
          LucideLayoutGrid,
          LucideMonitorSmartphone,
          LucidePencil,
          LucidePlus,
          LucideQrCode,
          LucideRadio,
          LucideRotateCw,
          LucideShield,
          LucideTrash2,
        ),
      ],
    });
    const fixture = TestBed.createComponent(NetworkPage);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    const buttons = fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>;
    const saveButton = Array.from(buttons).find(button => button.textContent?.includes('network.saveRestart') ?? false);
    expect(saveButton).toBeDefined();

    saveButton?.click();
    fixture.detectChanges();

    expect(fixture.componentInstance.restartDialogOpen()).toBe(true);
    expect(fixture.nativeElement.querySelector('dialog')?.open).toBe(true);
  });

  it('keeps the UI blocked until the restarted backend reports ready', async () => {
    let finishRestart: (() => void) | undefined;
    const restart = new Promise<void>(resolve => { finishRestart = resolve; });
    const api = {
      updateNetwork: vi.fn().mockResolvedValue({ restartRequired: true }),
      restartBackend: vi.fn().mockReturnValue(restart),
      getNetwork: vi.fn().mockResolvedValue(networkSettings()),
    };
    TestBed.configureTestingModule({ providers: [{ provide: BackendApiService, useValue: api }] });
    const page = TestBed.runInInjectionContext(() => new NetworkPage());
    page.restartDialogOpen.set(true);

    const operation = page.saveAndRestart();
    await vi.waitFor(() => expect(page.restartPhase()).toBe('restarting'));

    expect(page.saving()).toBe(true);
    expect(page.restartDialogOpen()).toBe(true);
    expect(page.notice()).toBe('');

    finishRestart?.();
    await operation;

    expect(page.saving()).toBe(false);
    expect(page.restartDialogOpen()).toBe(false);
    expect(page.notice()).toContain('restarted successfully');
    expect(api.getNetwork).toHaveBeenCalledOnce();
  });

  it('unblocks the UI and exposes the restart failure reason', async () => {
    const api = {
      updateNetwork: vi.fn().mockResolvedValue({ restartRequired: true }),
      restartBackend: vi.fn().mockRejectedValue(new Error('The configured port is already in use.')),
    };
    TestBed.configureTestingModule({ providers: [{ provide: BackendApiService, useValue: api }] });
    const page = TestBed.runInInjectionContext(() => new NetworkPage());
    page.restartDialogOpen.set(true);

    await page.saveAndRestart();

    expect(page.saving()).toBe(false);
    expect(page.restartDialogOpen()).toBe(false);
    expect(page.error()).toBe('The configured port is already in use.');
    expect(page.notice()).toBe('');
  });
});

function networkSettings(): NetworkSettings {
  return {
    mode: 0,
    addresses: ['127.0.0.1'],
    suggestedAddresses: ['localhost', '127.0.0.1'],
    port: 52024,
    discoveryEnabled: true,
    instanceName: 'Turbo Deck Backend',
    instanceId: 'test-instance',
    corsAllowedOrigins: [],
    allowInsecureHttp: false,
    httpsEnabled: false,
    certificatePath: '',
    certificatePasswordConfigured: false,
    lanApiKeyConfigured: false,
  };
}
