import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-auth-callback-page',
  imports: [],
  template: '<p class="auth-callback__status">Connexion en cours...</p>',
  styles: [
    '.auth-callback__status { margin: 2rem auto; max-width: 28rem; text-align: center; color: #4a5568; }'
  ]
})
export class AuthCallbackPageComponent implements OnInit {
  private readonly _router = inject(Router);
  private readonly _route = inject(ActivatedRoute);

  public ngOnInit(): void {
    const queryRedirect = this._route.snapshot.queryParamMap.get('redirectTo');
    const storedRedirect = sessionStorage.getItem('agenda.redirectTo');
    sessionStorage.removeItem('agenda.redirectTo');
    const redirectTo = this.normalizeRedirect(queryRedirect ?? storedRedirect);

    this._router.navigateByUrl(redirectTo, { replaceUrl: true });
  }

  private normalizeRedirect(redirectTo: string | null): string {
    if (redirectTo && redirectTo.startsWith('/') && !redirectTo.startsWith('//')) {
      return redirectTo;
    }

    return '/';
  }
}
