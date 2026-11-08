export {};

interface DesktopAppInfo {
  version: string;
  electronVersion: string;
  chromiumVersion: string;
  nodeVersion: string;
  packaged: boolean;
}

declare global {
  interface Window {
    turboDeck: {
      request<T>(method: 'GET' | 'POST' | 'PUT' | 'DELETE', path: string, body?: unknown): Promise<T>;
      selectAndUploadFile(purpose?: string): Promise<unknown | undefined>;
      getAppInfo(): Promise<DesktopAppInfo>;
    };
  }
}
