import { APP_INITIALIZER, ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { provideHttpClient } from '@angular/common/http';

import { providePrimeNG } from 'primeng/config';
import Aura from '@primeng/themes/aura';
import { InitService } from './core/services/init.service';
import { lastValueFrom } from 'rxjs';
import {provideAnimations} from '@angular/platform-browser/animations';
import {Noir} from '../../mytheme';

function initializeApp(initService: InitService) {
  return () => lastValueFrom(initService.init());
}


export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(),
    provideAnimations(),
    providePrimeNG({
      theme: {
        preset: Noir
      }
    }),
    {
      provide: APP_INITIALIZER,
      useFactory: initializeApp,
      multi:true,
      deps: [InitService]
    }
  ]
};
