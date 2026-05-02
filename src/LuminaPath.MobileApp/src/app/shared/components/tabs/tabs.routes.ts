import { Routes } from '@angular/router';
import { TabsPage } from './tabs.page';
import { AuthGuardService as AuthGuard } from '../../../core/middleware/auth-guard.service';

export const routes: Routes = [
  {
    path: '',
    component: TabsPage,
    canActivate: [AuthGuard],
    children: [
      {
        path: 'home',
        loadComponent: () =>
          import('../../../features/dashboard/pages/home.page').then((m) => m.HomePage),
        canActivate: [AuthGuard],
      },
      {
        path: 'media',
        loadComponent: () =>
          import('../../../features/media/pages/media.page').then((m) => m.MediaPage),
        canActivate: [AuthGuard],
      },
      {
        path: 'planing',
        loadComponent: () =>
          import('../../../features/planing/pages/planing.page').then((m) => m.PlaningPage),
        canActivate: [AuthGuard],
      },
      {
        path: '',
        redirectTo: '/home',
        pathMatch: 'full',
      },
    ],
  }
];
