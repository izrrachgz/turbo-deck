import { Routes } from '@angular/router';

import { AppRouteSegments, AppRouteTitleKeys } from './core/routing/app-route-constants';
import { AppShellComponent } from './layout/app-shell/app-shell.component';
import { unsavedChangesGuard } from './core/routing/unsaved-changes.guard';

export const APP_ROUTES: Routes = [
  {
    path: '',
    component: AppShellComponent,
    children: [
      {
        path: AppRouteSegments.deck,
        loadComponent: () =>
          import('./features/workspace/deck/pages/deck/deck.page').then((component) => component.DeckPage),
        data: { titleKey: AppRouteTitleKeys.deck },
      },
      {
        path: AppRouteSegments.queue,
        loadComponent: () =>
          import('./features/workspace/queue/pages/queue/queue.page').then((component) => component.QueuePage),
        data: { titleKey: AppRouteTitleKeys.queue },
      },
      {
        path: AppRouteSegments.twitch,
        loadComponent: () =>
          import('./features/connectivity/twitch/pages/twitch/twitch.page').then((component) => component.TwitchPage),
        data: { titleKey: AppRouteTitleKeys.twitch },
      },
      {
        path: AppRouteSegments.twitchSetup,
        loadComponent: () =>
          import('./features/connectivity/twitch-setup/pages/twitch-setup/twitch-setup.page').then(
            (component) => component.TwitchSetupPage,
          ),
        data: { titleKey: AppRouteTitleKeys.twitchSetup },
      },
      {
        path: AppRouteSegments.network,
        loadComponent: () =>
          import('./features/connectivity/network/pages/network/network.page').then((component) => component.NetworkPage),
        data: { titleKey: AppRouteTitleKeys.network },
      },
      {
        path: AppRouteSegments.settings,
        loadComponent: () =>
          import('./features/system/settings/pages/settings/settings.page').then((component) => component.SettingsPage),
        canDeactivate: [unsavedChangesGuard],
        data: { titleKey: AppRouteTitleKeys.settings },
      },
      {
        path: AppRouteSegments.advancedSettings,
        loadComponent: () =>
          import('./features/system/advanced-settings/pages/advanced-settings/advanced-settings.page').then(
            (component) => component.AdvancedSettingsPage,
          ),
        data: { titleKey: AppRouteTitleKeys.advancedSettings },
      },
      {
        path: AppRouteSegments.docs,
        loadComponent: () =>
          import('./features/system/docs/pages/docs/docs.page').then((component) => component.DocsPage),
        data: { titleKey: AppRouteTitleKeys.docs },
      },
      {
        path: AppRouteSegments.updates,
        loadComponent: () =>
          import('./features/system/updates/pages/updates/updates.page').then((component) => component.UpdatesPage),
        data: { titleKey: AppRouteTitleKeys.updates },
      },
      {
        path: AppRouteSegments.about,
        loadComponent: () =>
          import('./features/system/about/pages/about/about.page').then((component) => component.AboutPage),
        data: { titleKey: AppRouteTitleKeys.about },
      },
      { path: '', pathMatch: 'full', redirectTo: AppRouteSegments.deck },
    ],
  },
  { path: '**', redirectTo: AppRouteSegments.deck },
];
