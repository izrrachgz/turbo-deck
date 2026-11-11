import { Component } from '@angular/core';

import { TextPipe } from '../../../../../core/i18n/text.pipe';

@Component({
  selector: 'td-docs-page',
  standalone: true,
  imports: [TextPipe],
  templateUrl: './docs.page.html',
})
export class DocsPage {}
