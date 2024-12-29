import { Routes } from '@angular/router';
import { TabsPage } from './tabs.page';

export const routes: Routes = [
  {
    path: '',
    component: TabsPage,
    children: [
      {
        path: 'home',
        loadComponent: () =>
          import('../pages/home/home.page').then((m) => m.HomePage),
      },
      {
        path: 'media',
        loadComponent: () =>
          import('../pages/media/media.page').then((m) => m.MediaPage),
      },
      {
        path: 'planing',
        loadComponent: () =>
          import('../pages/planing/planing.page').then((m) => m.PlaningPage),
      },
      {
        path: '',
        redirectTo: '/home',
        pathMatch: 'full',
      },
    ],
  }
];
