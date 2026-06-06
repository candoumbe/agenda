import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AuthService } from '../../auth/auth.service';

@Component({
  selector: 'app-login-page',
  imports: [],
  templateUrl: './login-page.component.html',
  styleUrl: './login-page.component.css'
})
export class LoginPageComponent {
  private readonly _authService = inject(AuthService);
  private readonly _route = inject(ActivatedRoute);

  public startLogin(): void {
    const redirectTo = this._route.snapshot.queryParamMap.get('redirectTo');

    if (redirectTo) {
      sessionStorage.setItem('agenda.redirectTo', redirectTo);
    }

    this._authService.login();
  }
}
