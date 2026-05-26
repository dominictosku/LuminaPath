import { APP_INITIALIZER, ApplicationConfig, ErrorHandler, inject, provideZoneChangeDetection } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import {
  PreloadAllModules,
  RouteReuseStrategy,
  provideRouter,
  withPreloading,
} from '@angular/router';
import {
  IonicRouteStrategy,
  provideIonicAngular,
} from '@ionic/angular/standalone';
import {
  HTTP_INTERCEPTORS,
  provideHttpClient,
  withInterceptorsFromDi,
} from '@angular/common/http';
import { routes } from '../app/app.routes';
import { CookieInterceptor } from './core/middleware/CookieInterceptor.service';
import { GlobalErrorHandler } from './shared/services/global-error-handler';
import { RequestCache } from './shared/services/request-cache.service';

/** Upper bound on how long bootstrap will wait for the IndexedDB hydrate
 *  to land. On a healthy device the read is single-digit ms; this cap
 *  exists so a stuck IDB (rare: e.g. version-change blocked) cannot
 *  prevent the app from starting. The app still works fully without the
 *  hydrate — pages just don't paint cached data on first load. */
const CACHE_HYDRATE_TIMEOUT_MS = 1500;

function hydrateRequestCache(): () => Promise<void> {
  return async () => {
    const cache = inject(RequestCache);
    await Promise.race([
      cache.hydrate(),
      new Promise<void>((resolve) => setTimeout(resolve, CACHE_HYDRATE_TIMEOUT_MS)),
    ]);
  };
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes, withPreloading(PreloadAllModules)),
    provideAnimationsAsync(),
    { provide: RouteReuseStrategy, useClass: IonicRouteStrategy },
    provideHttpClient(withInterceptorsFromDi()),
    { provide: HTTP_INTERCEPTORS, useClass: CookieInterceptor, multi: true },
    { provide: ErrorHandler, useClass: GlobalErrorHandler },
    {
      // Pulls persisted view snapshots out of IndexedDB into RequestCache's
      // in-memory Map before any page mounts, so dashboard/statistic/library
      // can paint the last-known good state instantly on a cold launch.
      provide: APP_INITIALIZER,
      useFactory: hydrateRequestCache,
      multi: true,
    },
    provideIonicAngular(),
  ],
};
