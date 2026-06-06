export class User implements IUserInfo {
  userName: string;
  email: string;
  age: number;
  /**
   * Optional list of role names returned by the backend (e.g. "Administrator",
   * "Editor"). Stays empty if the server doesn't expose them — gated UI just
   * stays hidden in that case.
   */
  roles: string[];

  constructor() {
    this.userName = 'Please login';
    this.email = '';
    this.age = 0;
    this.roles = [];
  }
}

export class Credentials implements ICredentials {
  userName: string;
  email: string;
  password: string;
  twoFactorCode?: string;
  twoFactorRecoveryCode?: string;

  constructor() {
    this.userName = '';
    this.email = '';
    this.password = '';
  }
}

export interface IUserInfo {
  userName: string
  email: string
  age: number
  roles?: string[]
}

const ADMIN_ROLE_NAME = 'Administrator';
const CATALOG_EDITOR_ROLE_NAMES = [ADMIN_ROLE_NAME, 'Editor'] as const;

export function hasAdminRole(roles: readonly string[] | null | undefined): boolean {
  return hasAnyRole(roles, [ADMIN_ROLE_NAME]);
}

export function canEditCatalog(roles: readonly string[] | null | undefined): boolean {
  return hasAnyRole(roles, CATALOG_EDITOR_ROLE_NAMES);
}

function hasAnyRole(roles: readonly string[] | null | undefined, allowedRoles: readonly string[]): boolean {
  if (!roles?.length) return false;
  const allowed = new Set(allowedRoles.map((role) => role.toLowerCase()));
  return roles.some((role) => allowed.has(role.toLowerCase()));
}

export interface ICredentials {
  userName: string
  email: string
  password: string
  twoFactorCode?: string
  twoFactorRecoveryCode?: string
}
