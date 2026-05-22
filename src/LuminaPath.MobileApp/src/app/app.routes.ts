import { Routes } from '@angular/router';

/**
 * Top-level routing table.
 *
 * Everything authenticated lives inside the `TabsPage` shell — see
 * `tabs.routes.ts` for the per-tab declarations. Only the standalone auth
 * pages (which deliberately render *without* the tab bar / nav-bar) sit at
 * this level alongside the empty-path entry that hands the rest off to the
 * tabs router.
 */
export const routes: Routes = [
  {
    path: 'auth/login',
    loadComponent: () => import('./core/auth/pages/login/login.page').then(m => m.LoginPage),
  },
  {
    path: 'auth/create',
    loadComponent: () => import('./core/auth/pages/user-create/user-create.page').then(m => m.UserCreatePage),
  },
  {
    path: '',
    loadChildren: () => import('./shared/components/tabs/tabs.routes').then((m) => m.routes),
  },
];
