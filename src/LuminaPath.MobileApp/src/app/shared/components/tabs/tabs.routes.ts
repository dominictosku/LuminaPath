import { Routes } from '@angular/router';
import { TabsPage } from './tabs.page';
import { AuthGuard } from '../../../core/guards/auth.guard';
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
    canActivateChild: [AuthGuard],
    children: [
      {
        path: 'home',
        loadComponent: () =>
          import('../../../features/dashboard/pages/home.page').then((m) => m.HomePage),
      },
      {
        path: 'library',
        loadComponent: () =>
          import('../../../features/library/pages/library.page').then((m) => m.LibraryPage),
      },
      {
        path: 'browse',
        loadComponent: () =>
          import('../../../features/browse/pages/browse.page').then((m) => m.BrowsePage),
      },
      {
        path: 'library/games/:gameId',
        loadComponent: () =>
          import('../../../features/my-games/pages/my-game-details.page').then((m) => m.MyGameDetailsPage),
      },
      {
        path: 'library/animes/:animeId',
        loadComponent: loadEpisodicMediaDetailsPage,
        providers: animeDetailsProviders,
      },
      {
        path: 'library/movies/:movieId',
        loadComponent: () =>
          import('../../../features/movies/pages/movie-details.page').then((m) => m.MovieDetailsPage),
      },
      {
        path: 'library/series/:seriesId',
        loadComponent: loadEpisodicMediaDetailsPage,
        providers: seriesDetailsProviders,
      },
      {
        path: 'library/:gameId',
        loadComponent: () =>
          import('../../../features/my-games/pages/my-game-details.page').then((m) => m.MyGameDetailsPage),
      },
      {
        path: 'planning',
        loadComponent: () =>
          import('../../../features/planning/pages/planning.page').then((m) => m.PlanningPage),
      },
      {
        path: 'statistic',
        loadComponent: () =>
          import('../../../features/statistic/pages/statistic.page').then((m) => m.StatisticPage),
      },
      {
        path: 'quests',
        loadComponent: () =>
          import('../../../features/quests/pages/quest-board.page').then((m) => m.QuestBoardPage),
      },
      {
        path: 'skill-tree',
        loadComponent: () =>
          import('../../../features/skill-tree/pages/skill-tree.page').then((m) => m.SkillTreePage),
      },
      {
        path: 'notifications',
        loadComponent: () =>
          import('../../../features/notifications/pages/notifications.page').then((m) => m.NotificationsPage),
      },
      {
        path: 'profile',
        loadComponent: () =>
          import('../../../features/profile/pages/profile.page').then((m) => m.ProfilePage),
      },
      {
        path: 'friends',
        loadComponent: () =>
          import('../../../features/social/pages/friends.page').then((m) => m.FriendsPage),
      },
      {
        path: 'chat/:userId',
        loadComponent: () =>
          import('../../../features/social/pages/chat.page').then((m) => m.ChatPage),
      },
      {
        path: 'settings',
        loadComponent: () =>
          import('../../../features/profile/pages/settings.page').then((m) => m.SettingsPage),
      },
      {
        path: '',
        redirectTo: '/home',
        pathMatch: 'full',
      },
    ],
  }
];
