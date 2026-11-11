import { NgClass } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { LucideDynamicIcon } from '@lucide/angular';

import { TextPipe } from '../../core/i18n/text.pipe';
import { AppRoutePaths } from '../../core/routing/app-route-paths';
import { ConfirmationService } from '../../core/routing/confirmation.service';
import { ConfirmationDialogComponent } from '../../shared/components/confirmations/confirmation-dialog/confirmation-dialog.component';

interface NavigationItem {
  readonly labelKey: string;
  readonly icon: string;
  readonly route: string;
}

interface NavigationGroup {
  readonly labelKey: string;
  readonly items: readonly NavigationItem[];
}

@Component({
  selector: 'td-app-shell',
  standalone: true,
  imports: [ConfirmationDialogComponent, LucideDynamicIcon, NgClass, RouterLink, RouterLinkActive, RouterOutlet, TextPipe],
  templateUrl: './app-shell.component.html',
})
export class AppShellComponent {
  private static readonly donationUrl = 'https://www.paypal.com/donate/?hosted_button_id=5M8HPH943MVV4';

  readonly confirmations = inject(ConfirmationService);
  readonly navigation: readonly NavigationGroup[] = [
    {
      labelKey: 'nav.workspace',
      items: [
        { labelKey: 'nav.deck', icon: 'layout-dashboard', route: AppRoutePaths.deck },
        { labelKey: 'nav.queue', icon: 'list-tree', route: AppRoutePaths.queue },
      ],
    },
    {
      labelKey: 'nav.connectivity',
      items: [
        { labelKey: 'nav.twitch', icon: 'radio', route: AppRoutePaths.twitch },
        { labelKey: 'nav.network', icon: 'monitor-smartphone', route: AppRoutePaths.network },
      ],
    },
    {
      labelKey: 'nav.system',
      items: [
        { labelKey: 'nav.settings', icon: 'settings-2', route: AppRoutePaths.settings },
        { labelKey: 'nav.advancedSettings', icon: 'database-zap', route: AppRoutePaths.advancedSettings },
        { labelKey: 'nav.docs', icon: 'book-open', route: AppRoutePaths.docs },
        { labelKey: 'nav.updates', icon: 'refresh-cw', route: AppRoutePaths.updates },
        { labelKey: 'nav.feedback', icon: 'life-buoy', route: AppRoutePaths.feedback },
        { labelKey: 'nav.about', icon: 'info', route: AppRoutePaths.about },
      ],
    },
  ];

  mobileNavigationOpen = false;
  drawerCollapsed = false;

  openMobileNavigation(): void {
    this.drawerCollapsed = false;
    this.mobileNavigationOpen = true;
  }

  toggleDrawer(): void {
    this.drawerCollapsed = !this.drawerCollapsed;
  }

  openDonation(): void {
    this.mobileNavigationOpen = false;
    void window.turboDeck.openExternal(AppShellComponent.donationUrl);
  }
}
