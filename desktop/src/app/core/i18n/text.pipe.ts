import { ChangeDetectorRef, OnDestroy, Pipe, PipeTransform } from '@angular/core';
import { Subscription } from 'rxjs';

import { I18nService } from './i18n.service';

@Pipe({ name: 'text', standalone: true, pure: false })
export class TextPipe implements PipeTransform, OnDestroy {
  private key = '';
  private rendered = '';
  private readonly subscription: Subscription;

  constructor(
    private readonly i18n: I18nService,
    private readonly changeDetector: ChangeDetectorRef,
  ) {
    this.subscription = this.i18n.culture$.subscribe(() => {
      this.rendered = '';
      this.changeDetector.markForCheck();
    });
  }

  transform(key: string): string {
    if (this.key !== key || this.rendered === '') {
      this.key = key;
      this.rendered = this.i18n.text(key);
    }
    return this.rendered;
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }
}
