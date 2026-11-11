import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { BackendApiService } from '../../../../../core/backend-api.service';
import { TextPipe } from '../../../../../core/i18n/text.pipe';

@Component({
  selector: 'td-feedback-page',
  standalone: true,
  imports: [FormsModule, TextPipe],
  templateUrl: './feedback.page.html',
})
export class FeedbackPage {
  private readonly api = inject(BackendApiService);
  readonly error = signal('');
  readonly notice = signal('');
  readonly submitting = signal(false);
  category = 'Feedback';
  summary = '';
  description = '';

  async submit(): Promise<void> {
    this.submitting.set(true);
    this.error.set('');
    this.notice.set('');
    try {
      await this.api.submitFeedback(this.category, this.summary, this.description);
      this.notice.set('feedback.draftOpened');
    } catch (error) {
      this.error.set(error instanceof Error ? error.message : String(error));
    } finally {
      this.submitting.set(false);
    }
  }

  openIssue(): void {
    void this.api.openExternal('https://github.com/izrrachgz/turbo-deck/issues/new');
  }
}
