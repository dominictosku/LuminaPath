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

export const ADMIN_ROLE_NAMES = ['Administrator', 'Admin'] as const;
export const EDITOR_ROLE_NAMES = ['Editor'] as const;

export function hasAdminRole(roles: readonly string[] | null | undefined): boolean {
  if (!roles?.length) return false;
  const lowered = roles.map((role) => role.toLowerCase());
  return ADMIN_ROLE_NAMES.some((name) => lowered.includes(name.toLowerCase()));
}

export function canEditCatalog(roles: readonly string[] | null | undefined): boolean {
  if (!roles?.length) return false;
  const lowered = roles.map((role) => role.toLowerCase());
  return [...ADMIN_ROLE_NAMES, ...EDITOR_ROLE_NAMES].some((name) =>
    lowered.includes(name.toLowerCase()),
  );
}

export interface ICredentials {
  userName: string
  email: string
  password: string
  twoFactorCode?: string
  twoFactorRecoveryCode?: string
}
