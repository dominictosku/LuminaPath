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
        path: 'media/:gameId',
        loadComponent: () =>
          import('../../../features/my-games/pages/my-game-details.page').then((m) => m.MyGameDetailsPage),
        canActivate: [AuthGuard],
      },
      {
        path: 'planing',
        loadComponent: () =>
          import('../../../features/planing/pages/planing.page').then((m) => m.PlaningPage),
        canActivate: [AuthGuard],
      },
      {
        path: 'quests',
        loadComponent: () =>
          import('../../../features/quests/pages/quest-board.page').then((m) => m.QuestBoardPage),
        canActivate: [AuthGuard],
      },
      {
        path: 'profile',
        loadComponent: () =>
          import('../../../features/profile/pages/profile.page').then((m) => m.ProfilePage),
        canActivate: [AuthGuard],
      },
      {
        path: 'friends',
        loadComponent: () =>
          import('../../../features/social/pages/friends.page').then((m) => m.FriendsPage),
        canActivate: [AuthGuard],
      },
      {
        path: 'chat/:userId',
        loadComponent: () =>
          import('../../../features/social/pages/chat.page').then((m) => m.ChatPage),
        canActivate: [AuthGuard],
      },
      {
        path: 'settings',
        loadComponent: () =>
          import('../../../features/profile/pages/settings.page').then((m) => m.SettingsPage),
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
