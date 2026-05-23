import { canEditCatalog, hasAdminRole } from './user.model';

describe('hasAdminRole', () => {
  it('is false when roles are empty / null / undefined', () => {
    expect(hasAdminRole(null)).toBeFalse();
    expect(hasAdminRole(undefined)).toBeFalse();
    expect(hasAdminRole([])).toBeFalse();
  });

  it('matches "Administrator" case-insensitively', () => {
    expect(hasAdminRole(['Administrator'])).toBeTrue();
    expect(hasAdminRole(['administrator'])).toBeTrue();
    expect(hasAdminRole(['ADMINISTRATOR'])).toBeTrue();
  });

  it('also matches the short "Admin" alias', () => {
    expect(hasAdminRole(['Admin'])).toBeTrue();
    expect(hasAdminRole(['ADMIN'])).toBeTrue();
  });

  it('returns false for non-admin roles', () => {
    expect(hasAdminRole(['Editor'])).toBeFalse();
    expect(hasAdminRole(['User'])).toBeFalse();
    expect(hasAdminRole(['Member', 'Contributor'])).toBeFalse();
  });
});

describe('canEditCatalog', () => {
  it('is true for both Administrator and Editor', () => {
    expect(canEditCatalog(['Editor'])).toBeTrue();
    expect(canEditCatalog(['Administrator'])).toBeTrue();
    expect(canEditCatalog(['Admin', 'User'])).toBeTrue();
  });

  it('is false when no editor / admin role is present', () => {
    expect(canEditCatalog([])).toBeFalse();
    expect(canEditCatalog(null)).toBeFalse();
    expect(canEditCatalog(['User', 'Contributor'])).toBeFalse();
  });
});
