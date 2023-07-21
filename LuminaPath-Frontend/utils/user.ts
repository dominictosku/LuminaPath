export class User implements UserInfo {
    userName: string;
    age: number;
  
    constructor() {
      this.userName = '';
      this.age = 0
    }
}

export class Creds implements Credentials {
    userName: string;
    email: string;
    password: string
  
    constructor() {
      this.userName = '';
      this.email = '';
      this.password = '';
    }
}

export interface UserInfo {
    userName: string
    age: number
}

export interface Credentials {
    userName: string
    email: string
    password: string
}