import { Injectable, inject } from '@angular/core';
import { CanActivate, CanActivateChild, Router, UrlTree } from '@angular/router';
import { AuthService } from '../auth/services/auth.service';
import { map, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate, CanActivateChild {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  canActivate(): Observable<boolean | UrlTree> {
    return this.authorize();
  }

  canActivateChild(): Observable<boolean | UrlTree> {
    return this.authorize();
  }

  private authorize(): Observable<boolean | UrlTree> {
    return this.auth.isLoggedIn().pipe(
      map((isLoggedIn) => {
        return isLoggedIn ? true : this.router.createUrlTree(['/auth/login']);
      })
    );
  }
}
