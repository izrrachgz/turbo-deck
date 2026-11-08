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
  port: number;
  discoveryEnabled: boolean;
  instanceName: string;
  instanceId: string;
  corsAllowedOrigins: string[];
  allowInsecureHttp: boolean;
  httpsEnabled: boolean;
  certificatePath: string;
  certificatePasswordConfigured: boolean;
}

export interface DesktopAppInfo {
  version: string;
  electronVersion: string;
  chromiumVersion: string;
  nodeVersion: string;
  packaged: boolean;
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

  getFiles(): Promise<StoredFile[]> {
    return this.request('GET', '/files?limit=100');
  }

  uploadFile(purpose?: string): Promise<StoredFile | undefined> {
    return window.turboDeck.selectAndUploadFile(purpose) as Promise<StoredFile | undefined>;
  }

  deleteFile(id: number): Promise<void> {
    return this.request('DELETE', `/files/${id}`);
  }

  private request<T>(method: 'GET' | 'POST' | 'PUT' | 'DELETE', path: string, body?: unknown): Promise<T> {
    return window.turboDeck.request<T>(method, path, body);
  }
}
