import { Route, Routes } from '@angular/router';

import { routes } from './app.routes';

function topLevelRoute(path: string): Route {
  const route = routes.find((item) => item.path === path);
  if (!route) {
    fail(`Top-level route '${path}' was not registered.`);
  }
  return route!;
}

async function loadTabRoutes(): Promise<Routes> {
  const empty = topLevelRoute('');
  expect(empty.loadChildren).withContext('empty path should hand off to the tabs route tree').toEqual(jasmine.any(Function));
  const children = (await (empty.loadChildren as () => Promise<Routes>)()) as Routes;
  expect(Array.isArray(children)).withContext('tabs.routes default export should be an array').toBeTrue();
  return children;
}

function tabChildRoute(parents: Routes, path: string): Route {
  // The tabs file wraps every page in a single `path: ''` route whose
  // `children` array carries the per-tab declarations. Walk one level in.
  const shell = parents.find((item) => item.path === '');
  if (!shell || !shell.children) {
    fail(`Tabs route shell with empty path + children not found.`);
    return {} as Route;
  }
  const route = shell.children.find((item) => item.path === path);
  if (!route) {
    fail(`Tab route '${path}' was not registered.`);
  }
  return route!;
}

async function resolveTabComponent(parents: Routes, path: string): Promise<unknown> {
  const route = tabChildRoute(parents, path);
  expect(route.loadComponent).toEqual(jasmine.any(Function));
  return await (route.loadComponent as () => Promise<unknown>)();
}

async function resolveTopLevelComponent(path: string): Promise<unknown> {
  const route = topLevelRoute(path);
  expect(route.loadComponent).toEqual(jasmine.any(Function));
  return await (route.loadComponent as () => Promise<unknown>)();
}

describe('App routes smoke', () => {
  it('lazy-loads the main tab route tree', async () => {
    const tabRoutes = await loadTabRoutes();
    expect(tabRoutes.some((route) => route.path === '' && Array.isArray(route.children))).toBeTrue();
  });

  it('keeps the critical authenticated tab routes registered behind the auth guard', async () => {
    const tabRoutes = await loadTabRoutes();
    const shell = tabRoutes.find((item) => item.path === '');
    expect(shell?.canActivate?.length).withContext('tabs shell canActivate').toBeGreaterThan(0);
    expect(shell?.canActivateChild?.length).withContext('tabs shell canActivateChild').toBeGreaterThan(0);

    const protectedPaths = [
      'home',
      'library',
      'browse',
      'library/games/:gameId',
      'planning',
      'statistic',
      'quests',
      'skill-tree',
      'notifications',
      'profile',
      'friends',
      'settings',
    ];

    for (const path of protectedPaths) {
      const route = tabChildRoute(tabRoutes, path);
      expect(route.loadComponent).withContext(path).toEqual(jasmine.any(Function));
    }
  });

  it('lazy-loads the main app pages', async () => {
    const tabRoutes = await loadTabRoutes();
    await expectAsync(Promise.all([
      resolveTabComponent(tabRoutes, 'home'),
      resolveTabComponent(tabRoutes, 'library'),
      resolveTabComponent(tabRoutes, 'browse'),
      resolveTabComponent(tabRoutes, 'library/games/:gameId'),
      resolveTabComponent(tabRoutes, 'planning'),
      resolveTabComponent(tabRoutes, 'statistic'),
      resolveTabComponent(tabRoutes, 'quests'),
      resolveTabComponent(tabRoutes, 'skill-tree'),
      resolveTabComponent(tabRoutes, 'notifications'),
      resolveTabComponent(tabRoutes, 'profile'),
      resolveTabComponent(tabRoutes, 'friends'),
      resolveTabComponent(tabRoutes, 'settings'),
    ])).toBeResolved();
  });

  it('lazy-loads the authentication pages at the top level (no tabs shell)', async () => {
    await expectAsync(Promise.all([
      resolveTopLevelComponent('auth/login'),
      resolveTopLevelComponent('auth/create'),
    ])).toBeResolved();
  });
});
