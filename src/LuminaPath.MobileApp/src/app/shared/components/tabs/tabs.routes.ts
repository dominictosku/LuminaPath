import { Routes } from '@angular/router';
import { TabsPage } from './tabs.page';
import { AuthGuardService as AuthGuard } from '../../../core/middleware/auth-guard.service';
import { animeDetailsProviders } from '../../../features/animes/pages/anime-details.providers';
import { seriesDetailsProviders } from '../../../features/series/pages/series-details.providers';

const loadEpisodicMediaDetailsPage = () =>
  import('../../../features/library/pages/episodic-media-details/episodic-media-details.page').then(
    (m) => m.EpisodicMediaDetailsPage,
  );

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
        path: 'library',
        loadComponent: () =>
          import('../../../features/library/pages/library.page').then((m) => m.LibraryPage),
        canActivate: [AuthGuard],
      },
      {
        path: 'browse',
        loadComponent: () =>
          import('../../../features/browse/pages/browse.page').then((m) => m.BrowsePage),
        canActivate: [AuthGuard],
      },
      {
        path: 'library/games/:gameId',
        loadComponent: () =>
          import('../../../features/my-games/pages/my-game-details.page').then((m) => m.MyGameDetailsPage),
        canActivate: [AuthGuard],
      },
      {
        path: 'library/animes/:animeId',
        loadComponent: loadEpisodicMediaDetailsPage,
        providers: animeDetailsProviders,
        canActivate: [AuthGuard],
      },
      {
        path: 'library/movies/:movieId',
        loadComponent: () =>
          import('../../../features/movies/pages/movie-details.page').then((m) => m.MovieDetailsPage),
        canActivate: [AuthGuard],
      },
      {
        path: 'library/series/:seriesId',
        loadComponent: loadEpisodicMediaDetailsPage,
        providers: seriesDetailsProviders,
        canActivate: [AuthGuard],
      },
      {
        path: 'library/:gameId',
        loadComponent: () =>
          import('../../../features/my-games/pages/my-game-details.page').then((m) => m.MyGameDetailsPage),
        canActivate: [AuthGuard],
      },
      {
        path: 'planning',
        loadComponent: () =>
          import('../../../features/planning/pages/planning.page').then((m) => m.PlanningPage),
        canActivate: [AuthGuard],
      },
      {
        path: 'planing',
        redirectTo: 'planning',
        pathMatch: 'full',
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
