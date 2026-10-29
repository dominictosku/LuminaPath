import { inject } from '@angular/core';
import { Router, Routes } from '@angular/router';
import { MediaKind } from './models/media-item.model';

export const libraryRoutes: Routes = [
  {
    path: 'library',
    loadComponent: () => import('./pages/library.page').then((m) => m.LibraryPage),
  },
  ...([
    ['games', 'gameId'],
    ['animes', 'animeId'],
    ['movies', 'movieId'],
    ['series', 'seriesId'],
  ] as const).map(([kind, param]) => detailRedirect(`library/${kind}/:${param}`, kind, param)),
  detailRedirect('library/:gameId', 'games', 'gameId'),
];

function detailRedirect(path: string, kind: MediaKind, param: string): Routes[number] {
  return {
    path,
    pathMatch: 'full',
    redirectTo: ({ params, queryParams }) => inject(Router).createUrlTree(['/library'], {
      queryParams: { ...queryParams, mediaKind: kind, mediaId: params[param] },
    }),
  };
}
