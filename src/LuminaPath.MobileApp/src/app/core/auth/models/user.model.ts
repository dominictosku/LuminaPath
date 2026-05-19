export class User implements IUserInfo {
  userName: string;
  email: string;
  age: number;

  constructor() {
    this.userName = 'Please login';
    this.email = '';
    this.age = 0
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
}

export interface ICredentials {
  userName: string
  email: string
  password: string
  twoFactorCode?: string
  twoFactorRecoveryCode?: string
}
