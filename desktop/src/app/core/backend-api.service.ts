import { Injectable } from '@angular/core';

export interface HealthStatus {
  status: string;
}

export interface StoredFile {
  id: number;
  originalFileName: string;
  contentType: string;
  extension: string;
  size: number;
  sha256: string;
  purpose?: string | null;
  createdUtc: string;
  modifiedUtc: string;
}

export interface NetworkSettings {
  mode: number;
  addresses: string[];
  suggestedAddresses: string[];
  port: number;
  discoveryEnabled: boolean;
  instanceName: string;
  instanceId: string;
  corsAllowedOrigins: string[];
  allowInsecureHttp: boolean;
  httpsEnabled: boolean;
  certificatePath: string;
  certificatePasswordConfigured: boolean;
  lanApiKeyConfigured: boolean;
}

export interface UpdateNetworkRequest {
  mode: number;
  addresses: string[];
  port: number;
  discoveryEnabled: boolean;
  instanceName: string;
  corsAllowedOrigins: string[];
  allowInsecureHttp: boolean;
  https: { enabled: boolean; certificatePath: string; certificatePassword: string };
}

export interface LanAccessModel {
  apiKey: string;
  baseUrls: string[];
}

export type DatabaseEngine = 'sqlite' | 'mssql' | 'pgsql' | 'mysql';
export type DatabaseConnectionStatus = 'Connected' | 'Idle' | 'Unconfigured' | 'Error';

export interface DatabaseEngineStateModel {
  engine: DatabaseEngine;
  displayName: string;
  status: DatabaseConnectionStatus;
  configured: boolean;
  connectionSummary?: string | null;
  lastError?: string | null;
}

export interface DatabaseSettingsModel {
  activeEngine: DatabaseEngine;
  engines: DatabaseEngineStateModel[];
}

export interface DatabaseConnectionTestModel {
  success: boolean;
  status: DatabaseConnectionStatus;
  message: string;
  connectionSummary?: string | null;
}

export interface DatabaseSwitchModel {
  activeEngine: DatabaseEngine;
  status: DatabaseConnectionStatus;
  message: string;
}

export interface TwitchConnectionStatusModel {
  configured: boolean;
  authorized: boolean;
  connected: boolean;
  broadcasterDisplayName?: string | null;
  lastError?: string | null;
}

export interface TwitchDeviceAuthorizationModel {
  userCode: string;
  verificationUri: string;
  expiresUtc: string;
  pollIntervalSeconds: number;
}

export interface TwitchAuthorizationPollModel {
  pending: boolean;
  status: TwitchConnectionStatusModel;
}

export interface TwitchRewardModel {
  id: string;
  title: string;
  cost: number;
  enabled: boolean;
  prompt?: string | null;
}

export interface TwitchRewardBindingModel {
  id: number;
  rewardId: string;
  rewardTitle: string;
  cellConfigId: number;
  enabled: boolean;
  createdUtc: string;
  modifiedUtc: string;
}

export interface DesktopAppInfo {
  version: string;
  electronVersion: string;
  chromiumVersion: string;
  nodeVersion: string;
  platform: string;
  architecture: string;
  packaged: boolean;
}

export interface DesktopUpdateStatus {
  phase: 'idle' | 'development' | 'test' | 'checking' | 'current' | 'available' | 'downloading' | 'verifying' | 'ready' | 'error';
  currentVersion: string;
  availableVersion?: string | null;
  progressPercent?: number | null;
  message?: string | null;
}

export interface ProfileModel {
  id: number;
  name: string;
  sortOrder: number;
  selectedPageNumber: number;
  createdUtc: string;
  modifiedUtc: string;
}

export interface DeckPageModel {
  id: number;
  profileId: number;
  name: string;
  position: number;
  createdUtc: string;
  modifiedUtc: string;
}

export type CellAnimationType = 'none' | 'fade' | 'slide' | 'zoom' | 'spin' | 'flip' | 'bounce' | 'morph' | 'orbit' | 'blur';
export type CellDisplaySize = 'small' | 'medium' | 'large' | 'fullscreen';

export interface CellConfigModel {
  id: number;
  pageId: number;
  rowIndex: number;
  columnIndex: number;
  label?: string | null;
  enabled: boolean;
  mediaAssetId?: number | null;
  thumbnailMediaAssetId?: number | null;
  volume: number;
  playbackRate: number;
  pitchSemitones: number;
  displayDurationMilliseconds: number;
  displayPermanently: boolean;
  displaySize: CellDisplaySize;
  animationInType: CellAnimationType;
  animationInDurationMilliseconds: number;
  animationOutType: CellAnimationType;
  animationOutDurationMilliseconds: number;
  queuePriority: 'normal' | 'override';
  createdUtc: string;
  modifiedUtc: string;
}

export interface MediaAssetModel {
  id: number;
  displayName: string;
  kind: 'image' | 'audio' | 'video' | 'unknown';
  inspectionStatus: string;
  width?: number | null;
  height?: number | null;
  durationMilliseconds?: number | null;
  videoCodec?: string | null;
  audioCodec?: string | null;
  hasAudio: boolean;
  waveformData?: string | null;
  file: StoredFile;
  createdUtc: string;
  modifiedUtc: string;
}

export interface PlaybackQueueItemModel {
  id: string;
  cellConfigId: number;
  mediaAssetId: number;
  label: string;
  kind: string;
  volume: number;
  playbackRate: number;
  pitchSemitones: number;
  displayDurationMilliseconds: number;
  displayPermanently: boolean;
  displaySize: CellDisplaySize;
  animationInType: CellAnimationType;
  animationInDurationMilliseconds: number;
  animationOutType: CellAnimationType;
  animationOutDurationMilliseconds: number;
  waveformData?: string | null;
  width?: number | null;
  height?: number | null;
  sourceDurationMilliseconds?: number | null;
  priority: 'normal' | 'override';
  queuedUtc: string;
}

export interface PlaybackQueueSnapshotModel {
  current?: PlaybackQueueItemModel | null;
  pending: PlaybackQueueItemModel[];
}

export interface UpdateCellRequest {
  label: string;
  enabled: boolean;
  mediaAssetId?: number | null;
  thumbnailMediaAssetId?: number | null;
  volume: number;
  playbackRate: number;
  pitchSemitones: number;
  displayDurationMilliseconds: number;
  displayPermanently: boolean;
  displaySize: CellDisplaySize;
  animationInType: CellAnimationType;
  animationInDurationMilliseconds: number;
  animationOutType: CellAnimationType;
  animationOutDurationMilliseconds: number;
  queuePriority: 'normal' | 'override';
}

export interface DeckGridModel {
  page: DeckPageModel;
  cells: CellConfigModel[];
}

export interface AppSettingsModel {
  id: number;
  activeProfileId?: number | null;
  culture: 'en-US' | 'es-ES';
  theme: 'dark' | 'light';
  palette: string;
  timeZone: string;
  queueCapacity: number;
  visualizerSafeMargin: number;
  createdUtc: string;
  modifiedUtc: string;
}

@Injectable({ providedIn: 'root' })
export class BackendApiService {
  getAppInfo(): Promise<DesktopAppInfo> {
    return window.turboDeck.getAppInfo();
  }

  getHealth(): Promise<HealthStatus> {
    return this.request('GET', '/health/ready');
  }

  getNetwork(): Promise<NetworkSettings> {
    return this.request('GET', '/system/network');
  }

  getDatabaseSettings(): Promise<DatabaseSettingsModel> {
    return this.request('GET', '/system/database');
  }

  testDatabaseConnection(engine: DatabaseEngine, connectionString: string): Promise<DatabaseConnectionTestModel> {
    return this.request('POST', '/system/database/test', { engine, connectionString: connectionString.trim() || null });
  }

  switchDatabaseEngine(engine: DatabaseEngine, connectionString: string): Promise<DatabaseSwitchModel> {
    return this.request('POST', '/system/database/switch', { engine, connectionString: connectionString.trim() || null });
  }

  updateNetwork(settings: UpdateNetworkRequest): Promise<{ restartRequired: boolean }> {
    return this.request('PUT', '/system/network', settings);
  }

  restartBackend(): Promise<void> {
    return window.turboDeck.restartBackend();
  }

  createLanAccess(): Promise<LanAccessModel> {
    return window.turboDeck.createLanAccess();
  }

  configureFirewall(port: number, action: 'add' | 'remove'): Promise<{ applied: boolean }> {
    return window.turboDeck.configureFirewall(port, action);
  }

  getTwitchStatus(): Promise<TwitchConnectionStatusModel> {
    return this.request('GET', '/api/v1/twitch/status');
  }

  startTwitchAuthorization(clientId: string): Promise<TwitchDeviceAuthorizationModel> {
    return this.request('POST', '/api/v1/twitch/device-authorization', { clientId });
  }

  pollTwitchAuthorization(): Promise<TwitchAuthorizationPollModel> {
    return this.request('POST', '/api/v1/twitch/device-authorization/poll');
  }

  disconnectTwitch(): Promise<void> {
    return this.request('POST', '/api/v1/twitch/disconnect');
  }

  getTwitchRewards(): Promise<TwitchRewardModel[]> {
    return this.request('GET', '/api/v1/twitch/rewards');
  }

  getTwitchBindings(): Promise<TwitchRewardBindingModel[]> {
    return this.request('GET', '/api/v1/twitch/bindings');
  }

  saveTwitchBinding(reward: TwitchRewardModel, cellConfigId: number, enabled: boolean): Promise<TwitchRewardBindingModel> {
    return this.request('PUT', `/api/v1/twitch/bindings/${encodeURIComponent(reward.id)}`, {
      rewardTitle: reward.title,
      cellConfigId,
      enabled,
    });
  }

  deleteTwitchBinding(rewardId: string): Promise<void> {
    return this.request('DELETE', `/api/v1/twitch/bindings/${encodeURIComponent(rewardId)}`);
  }

  openExternal(url: string): Promise<void> {
    return window.turboDeck.openExternal(url);
  }

  getLicense(): Promise<string> {
    return window.turboDeck.getLicense();
  }

  submitFeedback(category: string, summary: string, description: string): Promise<void> {
    return window.turboDeck.submitFeedback({ category, summary, description });
  }

  quitApplication(): Promise<void> {
    return window.turboDeck.quit();
  }

  getUpdateStatus(): Promise<DesktopUpdateStatus> {
    return window.turboDeck.getUpdateStatus();
  }

  checkForUpdates(): Promise<DesktopUpdateStatus> {
    return window.turboDeck.checkForUpdates();
  }

  downloadUpdate(): Promise<DesktopUpdateStatus> {
    return window.turboDeck.downloadUpdate();
  }

  applyUpdate(): Promise<void> {
    return window.turboDeck.applyUpdate();
  }

  getFiles(): Promise<StoredFile[]> {
    return this.request('GET', '/files?limit=100');
  }

  uploadFile(purpose?: string): Promise<StoredFile | undefined> {
    return window.turboDeck.selectAndUploadFile(purpose) as Promise<StoredFile | undefined>;
  }

  deleteFile(id: number): Promise<void> {
    return this.request('DELETE', `/files/${id}`);
  }

  getProfiles(): Promise<ProfileModel[]> {
    return this.request('GET', '/api/v1/profiles');
  }

  createProfile(name: string): Promise<ProfileModel> {
    return this.request('POST', '/api/v1/profiles', { name });
  }

  getPages(profileId: number): Promise<DeckPageModel[]> {
    return this.request('GET', `/api/v1/profiles/${profileId}/pages`);
  }

  getGrid(pageId: number): Promise<DeckGridModel> {
    return this.request('GET', `/api/v1/pages/${pageId}/grid`);
  }

  updateCell(pageId: number, rowIndex: number, columnIndex: number, cell: UpdateCellRequest): Promise<CellConfigModel> {
    return this.request('PUT', `/api/v1/pages/${pageId}/cells/${rowIndex}/${columnIndex}`, cell);
  }

  getMediaAssets(): Promise<MediaAssetModel[]> {
    return this.request('GET', '/api/v1/media-assets?limit=100');
  }

  uploadMedia(displayName?: string, mediaKind: 'any' | 'image' = 'any'): Promise<MediaAssetModel | undefined> {
    return window.turboDeck.selectAndUploadMedia(displayName, mediaKind) as Promise<MediaAssetModel | undefined>;
  }

  getPlaybackQueue(): Promise<PlaybackQueueSnapshotModel> {
    return this.request('GET', '/api/v1/playback/queue');
  }

  queueCell(cellConfigId: number): Promise<PlaybackQueueSnapshotModel> {
    return this.request('POST', '/api/v1/playback/queue', { cellConfigId });
  }

  completePlayback(itemId: string, success = true, errorCode?: string): Promise<PlaybackQueueSnapshotModel> {
    return this.request('POST', '/api/v1/playback/complete', { itemId, success, errorCode });
  }

  removePlaybackQueueItem(itemId: string): Promise<PlaybackQueueSnapshotModel> {
    return this.request('DELETE', `/api/v1/playback/queue/${itemId}`);
  }

  movePlaybackQueueItem(itemId: string, offset: -1 | 1): Promise<PlaybackQueueSnapshotModel> {
    return this.request('POST', `/api/v1/playback/queue/${itemId}/move`, { offset });
  }

  clearPlaybackQueue(stopCurrent = false): Promise<PlaybackQueueSnapshotModel> {
    return this.request('POST', '/api/v1/playback/clear', { stopCurrent });
  }

  openVisualizer(): Promise<void> {
    return window.turboDeck.openVisualizer();
  }

  openDeckExplorer(): Promise<void> {
    return window.turboDeck.openDeckExplorer();
  }

  onPlaybackQueueChanged(callback: () => void): () => void {
    return window.turboDeck.onPlaybackQueueChanged(callback);
  }

  getSettings(): Promise<AppSettingsModel> {
    return this.request('GET', '/api/v1/settings');
  }

  updateSettings(settings: Omit<AppSettingsModel, 'id' | 'createdUtc' | 'modifiedUtc'>): Promise<AppSettingsModel> {
    return this.request('PUT', '/api/v1/settings', settings);
  }

  private request<T>(method: 'GET' | 'POST' | 'PUT' | 'DELETE', path: string, body?: unknown): Promise<T> {
    return window.turboDeck.request<T>(method, path, body);
  }
}
