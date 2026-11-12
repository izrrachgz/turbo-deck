import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { BackendApiService } from '../../../../../core/backend-api.service';
import { TwitchSetupPage } from './twitch-setup.page';

describe('TwitchSetupPage', () => {
  it('opens the Twitch application console in the system browser', () => {
    const api = { openExternal: vi.fn().mockResolvedValue(undefined) };
    TestBed.configureTestingModule({ providers: [{ provide: BackendApiService, useValue: api }] });
    const page = TestBed.runInInjectionContext(() => new TwitchSetupPage());

    page.openDeveloperConsole();

    expect(api.openExternal).toHaveBeenCalledWith('https://dev.twitch.tv/console/apps');
  });
});
