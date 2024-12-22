export class User implements IUserInfo {
  userName: string;
  age: number;

  constructor() {
    this.userName = 'Please login';
    this.age = 0
  }
}

export class Credentials implements ICredentials {
  userName: string;
  email: string;
  password: string

  constructor() {
    this.userName = '';
    this.email = '';
    this.password = '';
  }
}

export interface IUserInfo {
  userName: string
  age: number
}

export interface ICredentials {
  userName: string
  email: string
  password: string
}
