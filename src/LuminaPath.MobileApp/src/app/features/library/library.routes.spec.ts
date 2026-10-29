import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, RouteReuseStrategy, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { IonicRouteStrategy } from '@ionic/angular/standalone';
import { libraryRoutes } from './library.routes';

@Component({ template: '<p>Library</p>' })
class LibraryProbeComponent {}

describe('Library detail routing', () => {
  it('preserves the Library instance when opening and closing all legacy detail URLs', async () => {
    TestBed.configureTestingModule({ providers: [
      provideRouter(libraryRoutes.map(route => route.path === 'library'
        ? { path: 'library', component: LibraryProbeComponent } : route)),
      { provide: RouteReuseStrategy, useClass: IonicRouteStrategy },
    ] });
    const harness = await RouterTestingHarness.create();
    const initial = await harness.navigateByUrl('/library', LibraryProbeComponent);
    const router = TestBed.inject(Router);
    for (const kind of ['games', 'movies', 'animes', 'series']) {
      const detail = await harness.navigateByUrl(`/library/${kind}/42`, LibraryProbeComponent);
      expect(detail).toBe(initial);
      expect(router.parseUrl(router.url).queryParams).toEqual({ mediaKind: kind, mediaId: '42' });
      expect(await harness.navigateByUrl('/library', LibraryProbeComponent)).toBe(initial);
    }
    expect(await harness.navigateByUrl('/library/42', LibraryProbeComponent)).toBe(initial);
    expect(router.parseUrl(router.url).queryParams['mediaKind']).toBe('games');
  });
});
