import { NgClass } from '@angular/common';
import { Component, ElementRef, EventEmitter, Input, Output, ViewChild, AfterViewChecked } from '@angular/core';
import { LucideDynamicIcon } from '@lucide/angular';

import { TextPipe } from '../../../../core/i18n/text.pipe';

@Component({
  selector: 'td-confirmation-dialog',
  standalone: true,
  imports: [LucideDynamicIcon, NgClass, TextPipe],
  templateUrl: './confirmation-dialog.component.html',
})
export class ConfirmationDialogComponent implements AfterViewChecked {
  @ViewChild('dialog') private dialog?: ElementRef<HTMLDialogElement>;
  @Input() open = false;
  @Input() title = '';
  @Input() message = '';
  @Input() confirmLabel = 'action.confirm';
  @Input() cancelLabel = 'action.cancel';
  @Input() icon = 'circle-alert';
  @Input() destructive = false;
  @Input() processing = false;
  @Input() confirmDisabled = false;
  @Input() wide = false;
  @Input() allowOverflow = false;
  @Output() openChange = new EventEmitter<boolean>();
  @Output() confirmed = new EventEmitter<void>();

  ngAfterViewChecked(): void {
    const element = this.dialog?.nativeElement;
    if (this.open && element && !element.open) element.showModal();
    if (!this.open && element?.open) element.close();
  }

  cancel(): void {
    if (!this.processing) this.openChange.emit(false);
  }
}
