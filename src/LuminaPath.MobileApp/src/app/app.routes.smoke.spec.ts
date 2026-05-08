import { Route } from '@angular/router';

import { routes } from './app.routes';

function routeFor(path: string): Route {
  const route = routes.find((item) => item.path === path);
  if (!route) {
    fail(`Route '${path}' was not registered.`);
  }
  return route!;
}

async function resolveComponent(path: string): Promise<unknown> {
  const route = routeFor(path);
  expect(route.loadComponent).toEqual(jasmine.any(Function));
  return await (route.loadComponent as () => Promise<unknown>)();
}

describe('App routes smoke', () => {
  it('keeps the critical authenticated routes registered', () => {
    const protectedPaths = ['home', 'media', 'media/:gameId', 'planing', 'quests', 'profile', 'friends', 'settings'];

    for (const path of protectedPaths) {
      const route = routeFor(path);
      expect(route.canActivate?.length).withContext(path).toBeGreaterThan(0);
    }
  });

  it('lazy-loads the main tab route tree', async () => {
    const route = routeFor('');
    expect(route.loadChildren).toEqual(jasmine.any(Function));

    const childRoutes = await (route.loadChildren as () => Promise<unknown>)();
    expect(Array.isArray(childRoutes)).toBeTrue();
  });

  it('lazy-loads the main app pages', async () => {
    await expectAsync(Promise.all([
      resolveComponent('home'),
      resolveComponent('media'),
      resolveComponent('media/:gameId'),
      resolveComponent('planing'),
      resolveComponent('quests'),
      resolveComponent('profile'),
      resolveComponent('friends'),
      resolveComponent('settings'),
    ])).toBeResolved();
  });

  it('lazy-loads the authentication pages', async () => {
    await expectAsync(Promise.all([
      resolveComponent('auth/login'),
      resolveComponent('auth/create'),
    ])).toBeResolved();
  });
});
