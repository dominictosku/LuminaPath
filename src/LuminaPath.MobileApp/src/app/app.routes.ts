import { Routes } from '@angular/router';
import {
  AuthGuardService as AuthGuard
} from './core/middleware/auth-guard.service';

export const routes: Routes = [
  {
    path: '',
    loadChildren: () => import('./shared/components/tabs/tabs.routes').then((m) => m.routes),
  },
  {
    path: 'home',
    loadComponent: () => import('./features/dashboard/pages/home.page').then(m => m.HomePage),
    canActivate: [AuthGuard]
  },
  {
    path: 'media',
    loadComponent: () => import('./features/media/pages/media.page').then(m => m.MediaPage),
    canActivate: [AuthGuard]
  },
  {
    path: 'media/:gameId',
    loadComponent: () => import('./features/my-games/pages/my-game-details.page').then(m => m.MyGameDetailsPage),
    canActivate: [AuthGuard]
  },
  {
    path: 'planing',
    loadComponent: () => import('./features/planing/pages/planing.page').then(m => m.PlaningPage),
    canActivate: [AuthGuard]
  },
  {
    path: 'quests',
    loadComponent: () => import('./features/quests/pages/quest-board.page').then(m => m.QuestBoardPage),
    canActivate: [AuthGuard]
  },
  {
    path: 'profile',
    loadComponent: () => import('./features/profile/pages/profile.page').then(m => m.ProfilePage),
    canActivate: [AuthGuard]
  },
  {
    path: 'settings',
    loadComponent: () => import('./features/profile/pages/settings.page').then(m => m.SettingsPage),
    canActivate: [AuthGuard]
  },
  {
    path: 'auth/login',
    loadComponent: () => import('./core/auth/pages/login/login.page').then(m => m.LoginPage)
  },
  {
    path: 'auth/create',
    loadComponent: () => import('./core/auth/pages/user-create/user-create.page').then(m => m.UserCreatePage)
  },
  {
    path: 'test',
    loadComponent: () => import('./features/test/test.page').then(m => m.TestPage)
  },
];
