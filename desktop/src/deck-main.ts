import { bootstrapApplication } from '@angular/platform-browser';

import { LiveDeckPage } from './app/features/deck-explorer/pages/live-deck/live-deck.page';

bootstrapApplication(LiveDeckPage).catch(error => console.error(error));
