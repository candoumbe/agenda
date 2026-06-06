import { computed, inject, Injectable } from '@angular/core';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { catchError, of } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly _oidcSecurityService = inject(OidcSecurityService);

  public readonly isAuthenticated = computed(() => this._oidcSecurityService.authenticated().isAuthenticated);

  public readonly userName = computed(() => {
    const userData = this._oidcSecurityService.userData().userData as Record<string, unknown> | null;
    return this.resolveUserName(userData);
  });

  public login(): void {
    this._oidcSecurityService.authorize();
  }

  public logout(): void {
    this._oidcSecurityService.logoffAndRevokeTokens()
      .pipe(
        catchError(() => {
          this._oidcSecurityService.logoffLocal();
          return of(null);
        })
      )
      .subscribe();
  }

  private resolveUserName(userData: Record<string, unknown> | null): string {
    const preferredUserName = this.readStringClaim(userData, 'preferred_username');
    const fullName = this.readStringClaim(userData, 'name');
    const email = this.readStringClaim(userData, 'email');

    if (preferredUserName) {
      return preferredUserName;
    }

    if (fullName) {
      return fullName;
    }

    if (email) {
      return email;
    }

    return 'Mon compte';
  }

  private readStringClaim(userData: Record<string, unknown> | null, claimName: string): string {
    const claimValue = userData?.[claimName];
    if (typeof claimValue === 'string' && claimValue.trim().length > 0) {
      return claimValue;
    }

    return '';
  }
}
