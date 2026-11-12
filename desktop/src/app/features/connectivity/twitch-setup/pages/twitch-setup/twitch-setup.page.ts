import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideDynamicIcon } from '@lucide/angular';

import { BackendApiService } from '../../../../../core/backend-api.service';
import { TextPipe } from '../../../../../core/i18n/text.pipe';
import { AppRoutePaths } from '../../../../../core/routing/app-route-paths';

@Component({
  selector: 'td-twitch-setup-page',
  standalone: true,
  imports: [LucideDynamicIcon, RouterLink, TextPipe],
  templateUrl: './twitch-setup.page.html',
})
export class TwitchSetupPage {
  private static readonly developerConsoleUrl = 'https://dev.twitch.tv/console/apps';
  private readonly api = inject(BackendApiService);

  readonly twitchHubPath = AppRoutePaths.twitch;
  readonly steps = [1, 2, 3, 4, 5] as const;

  openDeveloperConsole(): void {
    void this.api.openExternal(TwitchSetupPage.developerConsoleUrl);
  }
}
