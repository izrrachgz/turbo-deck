import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LucideDynamicIcon } from '@lucide/angular';
import QRCode from 'qrcode';

import { BackendApiService, NetworkSettings, UpdateNetworkRequest } from '../../../../../core/backend-api.service';
import { TextPipe } from '../../../../../core/i18n/text.pipe';
import { ConfirmationDialogComponent } from '../../../../../shared/components/confirmations/confirmation-dialog/confirmation-dialog.component';
import {
  ListenerAddressError,
  listenerAddressErrors,
  listenerAddressKey,
  normalizeListenerAddresses,
  uniqueListenerAddresses,
} from '../../listener-address';

@Component({
  selector: 'td-network-page',
  standalone: true,
  imports: [ConfirmationDialogComponent, FormsModule, LucideDynamicIcon, TextPipe],
  templateUrl: './network.page.html',
})
export class NetworkPage implements OnInit {
  private readonly api = inject(BackendApiService);
  readonly settings = signal<NetworkSettings | null>(null);
  readonly error = signal('');
  readonly notice = signal('');
  readonly restartDialogOpen = signal(false);
  readonly saving = signal(false);
  readonly lanDialogOpen = signal(false);
  readonly lanAccess = signal<{ qr: string; payload: string; url: string } | null>(null);
  readonly firewallDialogOpen = signal(false);
  readonly firewallAction = signal<'add' | 'remove'>('add');
  readonly applyingFirewall = signal(false);
  readonly addressDialogOpen = signal(false);

  mode = 0;
  addresses = ['127.0.0.1'];
  addressDraft = ['127.0.0.1'];
  suggestedAddresses = ['localhost', '127.0.0.1', '0.0.0.0'];
  port = 52024;
  discoveryEnabled = false;
  instanceName = '';
  corsAllowedOrigins = '';
  allowInsecureHttp = false;
  httpsEnabled = false;
  certificatePath = '';
  certificatePassword = '';

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  async load(): Promise<void> {
    try {
      const settings = await this.api.getNetwork();
      this.settings.set(settings);
      this.mode = settings.mode;
      this.addresses = uniqueListenerAddresses(settings.addresses);
      if (!this.addresses.length) this.addresses = ['127.0.0.1'];
      this.suggestedAddresses = settings.suggestedAddresses;
      this.port = settings.port;
      this.discoveryEnabled = settings.discoveryEnabled;
      this.instanceName = settings.instanceName;
      this.corsAllowedOrigins = settings.corsAllowedOrigins.join('\n');
      this.allowInsecureHttp = settings.allowInsecureHttp;
      this.httpsEnabled = settings.httpsEnabled;
      this.certificatePath = settings.certificatePath;
      this.error.set('');
    } catch (error) {
      this.error.set(this.errorText(error));
    }
  }

  async saveAndRestart(): Promise<void> {
    this.saving.set(true);
    this.error.set('');
    try {
      if (this.mode === 1 && !this.settings()?.lanApiKeyConfigured) await this.api.createLanAccess();
      await this.api.updateNetwork(this.toRequest());
      this.restartDialogOpen.set(false);
      await this.api.restartBackend();
      this.notice.set('Network settings were saved. The backend is restarting.');
    } catch (error) {
      this.error.set(this.errorText(error));
    } finally {
      this.saving.set(false);
    }
  }

  async openVisualizer(): Promise<void> {
    this.error.set('');
    try {
      await this.api.openVisualizer();
    } catch (error) {
      this.error.set(this.errorText(error));
    }
  }

  async openDeckExplorer(): Promise<void> {
    this.error.set('');
    try {
      await this.api.openDeckExplorer();
    } catch (error) {
      this.error.set(this.errorText(error));
    }
  }

  async createLanPairing(): Promise<void> {
    this.error.set('');
    try {
      const access = await this.api.createLanAccess();
      if (!access.baseUrls.length) throw new Error('No LAN listener is active. Save LAN mode and restart first.');
      const url = access.baseUrls[0];
      const payload = JSON.stringify({ version: 1, baseUrl: url, apiKey: access.apiKey });
      const qr = await QRCode.toDataURL(payload, { errorCorrectionLevel: 'M', margin: 2, width: 320 });
      this.lanAccess.set({ qr, payload, url });
      this.lanDialogOpen.set(false);
    } catch (error) {
      this.error.set(this.errorText(error));
    }
  }

  confirmFirewall(action: 'add' | 'remove'): void {
    this.firewallAction.set(action);
    this.firewallDialogOpen.set(true);
  }

  openAddressDialog(): void {
    this.addressDraft = this.addresses.length ? [...this.addresses] : [''];
    this.addressDialogOpen.set(true);
  }

  addAddress(address = ''): void {
    const key = listenerAddressKey(address);
    if (key && this.addressDraft.some(current => listenerAddressKey(current) === key)) return;
    if (address && this.addressDraft.length === 1 && !this.addressDraft[0].trim()) {
      this.addressDraft = [address];
      return;
    }
    if (!address && this.addressDraft.some(current => !current.trim())) return;
    this.addressDraft = [...this.addressDraft, address];
  }

  removeAddress(index: number): void {
    this.addressDraft = this.addressDraft.filter((_, current) => current !== index);
    if (!this.addressDraft.length) this.addressDraft = [''];
  }

  saveAddresses(): void {
    if (this.addressEditorInvalid()) return;
    this.addresses = normalizeListenerAddresses(this.addressDraft);
    this.addressDialogOpen.set(false);
  }

  addressError(index: number): ListenerAddressError {
    return listenerAddressErrors(this.addressDraft)[index] ?? null;
  }

  addressErrorKey(index: number): string {
    const error = this.addressError(index);
    return error ? `network.addressError.${error}` : '';
  }

  addressEditorInvalid(): boolean {
    return !this.addressDraft.length || listenerAddressErrors(this.addressDraft).some(Boolean);
  }

  suggestionDisabled(address: string): boolean {
    const key = listenerAddressKey(address);
    return key !== null && this.addressDraft.some(current => listenerAddressKey(current) === key);
  }

  async applyFirewall(): Promise<void> {
    this.applyingFirewall.set(true);
    this.error.set('');
    try {
      await this.api.configureFirewall(this.port, this.firewallAction());
      this.firewallDialogOpen.set(false);
      this.notice.set(this.firewallAction() === 'add' ? 'Private-network firewall rule added.' : 'Firewall rule removed.');
    } catch (error) {
      this.error.set(this.errorText(error));
    } finally {
      this.applyingFirewall.set(false);
    }
  }

  private toRequest(): UpdateNetworkRequest {
    return {
      mode: this.mode,
      addresses: this.addresses,
      port: Number(this.port),
      discoveryEnabled: this.discoveryEnabled,
      instanceName: this.instanceName.trim(),
      corsAllowedOrigins: this.lines(this.corsAllowedOrigins),
      allowInsecureHttp: this.allowInsecureHttp,
      https: {
        enabled: this.httpsEnabled,
        certificatePath: this.certificatePath.trim(),
        certificatePassword: this.certificatePassword,
      },
    };
  }

  private lines(value: string): string[] {
    return value.split(/\r?\n|,/).map(item => item.trim()).filter(Boolean);
  }

  private errorText(error: unknown): string {
    return error instanceof Error ? error.message : String(error);
  }
}
