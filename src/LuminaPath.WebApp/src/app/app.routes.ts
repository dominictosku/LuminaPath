import { Routes } from '@angular/router';
import {
  AuthGuardService as AuthGuard
} from './core/middleware/auth-guard.service';

export const routes: Routes = [
  {
    path: '',
    loadChildren: () => import('./ui/tabs/tabs.routes').then((m) => m.routes),
  },
  {
    path: 'home',
    loadComponent: () => import('./ui/pages/home/home.page').then(m => m.HomePage)
  },
  {
    path: 'media',
    loadComponent: () => import('./ui/pages/media/media.page').then(m => m.MediaPage),
    canActivate: [AuthGuard]
  },
  {
    path: 'planing',
    loadComponent: () => import('./ui/pages/planing/planing.page').then(m => m.PlaningPage),
    canActivate: [AuthGuard]
  },
  {
    path: 'auth/login',
    loadComponent: () => import('./ui/pages/login/login.page').then(m => m.LoginPage)
  },
  {
    path: 'auth/create',
    loadComponent: () => import('./ui/pages/user-create/user-create.page').then(m => m.UserCreatePage)
  },
  {
    path: 'test',
    loadComponent: () => import('./ui/pages/test/test.page').then(m => m.TestPage)
  },
];
