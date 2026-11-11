import { TestBed } from '@angular/core/testing';
import { describe, expect, it, vi } from 'vitest';

import { BackendApiService, DatabaseSettingsModel } from '../../../../../core/backend-api.service';
import { ConfirmationService } from '../../../../../core/routing/confirmation.service';
import { AdvancedSettingsPage } from './advanced-settings.page';

describe('AdvancedSettingsPage', () => {
  it('loads the active engine without receiving a stored connection string', async () => {
    const api = { getDatabaseSettings: vi.fn().mockResolvedValue(databaseSettings()) };
    const page = createPage(api);

    await page.load();

    expect(page.selectedEngine()).toBe('sqlite');
    expect(page.settings()?.engines[0].connectionSummary).toBe('SQLite · turbo-deck.db');
    expect(page.controls.sqlite.value).toBe('');
  });

  it('shows clean connection-test feedback from the backend', async () => {
    const api = {
      testDatabaseConnection: vi.fn().mockResolvedValue({
        success: false,
        status: 'Error',
        message: 'The PostgreSQL database does not exist.',
      }),
    };
    const page = createPage(api);

    await page.testConnection('pgsql');

    expect(page.error()).toBe('The PostgreSQL database does not exist.');
    expect(page.results().pgsql?.status).toBe('Error');
  });

  it('requires confirmation before switching and clears the submitted secret afterward', async () => {
    const api = {
      switchDatabaseEngine: vi.fn().mockResolvedValue({
        activeEngine: 'mysql',
        status: 'Connected',
        message: 'MySQL is now the active database engine.',
      }),
      getDatabaseSettings: vi.fn().mockResolvedValue({ ...databaseSettings(), activeEngine: 'mysql' }),
    };
    const confirmations = { confirm: vi.fn().mockResolvedValue(true) };
    const page = createPage(api, confirmations);
    page.controls.mysql.setValue('Server=db;Password=secret;');

    await page.confirmSwitch('mysql');

    expect(confirmations.confirm).toHaveBeenCalledOnce();
    expect(api.switchDatabaseEngine).toHaveBeenCalledWith('mysql', 'Server=db;Password=secret;');
    expect(page.controls.mysql.value).toBe('');
    expect(page.message()).toContain('active database engine');
  });
});

function createPage(api: object, confirmations: object = { confirm: vi.fn() }): AdvancedSettingsPage {
  TestBed.configureTestingModule({
    providers: [
      { provide: BackendApiService, useValue: api },
      { provide: ConfirmationService, useValue: confirmations },
    ],
  });
  return TestBed.runInInjectionContext(() => new AdvancedSettingsPage());
}

function databaseSettings(): DatabaseSettingsModel {
  return {
    activeEngine: 'sqlite',
    engines: [
      {
        engine: 'sqlite',
        displayName: 'SQLite',
        status: 'Connected',
        configured: true,
        connectionSummary: 'SQLite · turbo-deck.db',
      },
      { engine: 'mssql', displayName: 'Microsoft SQL Server', status: 'Unconfigured', configured: false },
      { engine: 'pgsql', displayName: 'PostgreSQL', status: 'Unconfigured', configured: false },
      { engine: 'mysql', displayName: 'MySQL', status: 'Unconfigured', configured: false },
    ],
  };
}
