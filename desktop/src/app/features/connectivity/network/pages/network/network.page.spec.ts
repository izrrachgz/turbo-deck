import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { BackendApiService, NetworkSettings } from '../../../../../core/backend-api.service';
import { NetworkPage } from './network.page';

describe('NetworkPage', () => {
  it('publishes the asynchronously loaded discovery name through reactive state', async () => {
    const api = { getNetwork: vi.fn().mockResolvedValue(networkSettings()) };
    TestBed.configureTestingModule({ providers: [{ provide: BackendApiService, useValue: api }] });
    const page = TestBed.runInInjectionContext(() => new NetworkPage());

    await page.load();

    expect(page.instanceName()).toBe('Turbo Deck Backend');
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
