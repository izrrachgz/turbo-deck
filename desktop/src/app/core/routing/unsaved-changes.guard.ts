import { inject } from '@angular/core';
import { CanDeactivateFn } from '@angular/router';

import { ConfirmationService } from './confirmation.service';

export interface HasUnsavedChanges {
  hasUnsavedChanges(): boolean;
}

export const unsavedChangesGuard: CanDeactivateFn<HasUnsavedChanges> = (component) => {
  if (!component.hasUnsavedChanges()) return true;
  return inject(ConfirmationService).confirm({
    titleKey: 'confirmation.unsavedTitle',
    messageKey: 'confirmation.unsavedMessage',
    confirmLabelKey: 'action.discard',
    destructive: true,
  });
};
