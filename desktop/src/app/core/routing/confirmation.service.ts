import { Injectable, signal } from '@angular/core';

export interface ConfirmationRequest {
  readonly titleKey: string;
  readonly messageKey: string;
  readonly confirmLabelKey: string;
  readonly destructive: boolean;
}

@Injectable({ providedIn: 'root' })
export class ConfirmationService {
  readonly request = signal<ConfirmationRequest | null>(null);
  private resolver?: (confirmed: boolean) => void;

  confirm(request: ConfirmationRequest): Promise<boolean> {
    this.resolve(false);
    this.request.set(request);
    return new Promise<boolean>((resolve) => (this.resolver = resolve));
  }

  resolve(confirmed: boolean): void {
    this.request.set(null);
    this.resolver?.(confirmed);
    this.resolver = undefined;
  }
}
