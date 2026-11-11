import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { DomSanitizer, SafeStyle } from '@angular/platform-browser';
import { LucideDynamicIcon } from '@lucide/angular';

import { TextPipe } from '../../../core/i18n/text.pipe';

@Component({
  selector: 'td-media-cell',
  standalone: true,
  imports: [LucideDynamicIcon, TextPipe],
  templateUrl: './media-cell.component.html',
  styleUrl: './media-cell.component.css',
})
export class MediaCellComponent {
  private readonly sanitizer = inject(DomSanitizer);

  @Input({ required: true }) rowIndex = 0;
  @Input({ required: true }) columnIndex = 0;
  @Input() label?: string | null;
  @Input() thumbnailUrl?: string | null;
  @Input() editMode = false;
  @Input() disabled = false;
  @Output() activated = new EventEmitter<void>();

  thumbnailBackground(): SafeStyle | null {
    if (!this.thumbnailUrl || !/^turbo-deck-media:\/\/thumbnail\/[1-9]\d*$/.test(this.thumbnailUrl)) return null;
    return this.sanitizer.bypassSecurityTrustStyle(`url("${this.thumbnailUrl}")`);
  }
}
