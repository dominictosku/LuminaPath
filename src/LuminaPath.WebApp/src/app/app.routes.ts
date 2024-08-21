import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadChildren: () => import('./ui/tabs/tabs.routes').then((m) => m.routes),
  },
  {
    path: 'home',
    loadComponent: () => import('./ui/home/home.page').then(m => m.HomePage)
  },
  {
    path: 'media',
    loadComponent: () => import('./ui/media/media.page').then(m => m.MediaPage)
  },
  {
    path: 'planing',
    loadComponent: () => import('./ui/planing/planing.page').then(m => m.PlaningPage)
  },
];
