export {};

interface DesktopAppInfo {
  version: string;
  electronVersion: string;
  chromiumVersion: string;
  nodeVersion: string;
  platform: string;
  architecture: string;
  packaged: boolean;
}

interface DesktopUpdateStatus {
  phase: 'idle' | 'development' | 'test' | 'checking' | 'current' | 'available' | 'downloading' | 'verifying' | 'ready' | 'error';
  currentVersion: string;
  availableVersion?: string | null;
  progressPercent?: number | null;
  message?: string | null;
}

declare global {
  interface Window {
    turboDeck: {
      request<T>(method: 'GET' | 'POST' | 'PUT' | 'DELETE', path: string, body?: unknown): Promise<T>;
      selectAndUploadFile(purpose?: string): Promise<unknown | undefined>;
      selectAndUploadMedia(displayName?: string, mediaKind?: 'any' | 'image'): Promise<unknown | undefined>;
      openVisualizer(): Promise<void>;
      openDeckExplorer(): Promise<void>;
      onPlaybackQueueChanged(callback: () => void): () => void;
      createLanAccess(): Promise<{ apiKey: string; baseUrls: string[] }>;
      configureFirewall(port: number, action: 'add' | 'remove'): Promise<{ applied: boolean }>;
      openExternal(url: string): Promise<void>;
      getLicense(): Promise<string>;
      submitFeedback(feedback: { category: string; summary: string; description: string }): Promise<void>;
      quit(): Promise<void>;
      getUpdateStatus(): Promise<DesktopUpdateStatus>;
      checkForUpdates(): Promise<DesktopUpdateStatus>;
      downloadUpdate(): Promise<DesktopUpdateStatus>;
      applyUpdate(): Promise<void>;
      getAppInfo(): Promise<DesktopAppInfo>;
    };
  }
}
