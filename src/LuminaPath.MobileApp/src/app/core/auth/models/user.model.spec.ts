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

  it('returns false for non-admin roles', () => {
    expect(hasAdminRole(['Editor'])).toBeFalse();
    expect(hasAdminRole(['Admin'])).toBeFalse();
    expect(hasAdminRole(['User'])).toBeFalse();
    expect(hasAdminRole(['Member', 'Contributor'])).toBeFalse();
  });
});

describe('canEditCatalog', () => {
  it('is true for both Administrator and Editor', () => {
    expect(canEditCatalog(['Editor'])).toBeTrue();
    expect(canEditCatalog(['Administrator'])).toBeTrue();
  });

  it('is false when no editor / admin role is present', () => {
    expect(canEditCatalog([])).toBeFalse();
    expect(canEditCatalog(null)).toBeFalse();
    expect(canEditCatalog(['Admin', 'User'])).toBeFalse();
    expect(canEditCatalog(['User', 'Contributor'])).toBeFalse();
  });
});
