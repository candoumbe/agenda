import { Component, computed, inject } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { TopbarComponent } from './components/topbar/topbar.component';
import { AuthService } from './auth/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, TopbarComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  private readonly _authService = inject(AuthService);
  private readonly _router = inject(Router);

  public readonly isAuthenticated = this._authService.isAuthenticated;
  public readonly accountName = this._authService.userName;
  public readonly shouldShowTopbar = computed(() => this.isAuthenticated());

  public handleAuthAction(): void {
    if (this.isAuthenticated()) {
      this._authService.logout();
      this._router.navigate(['/login'], { replaceUrl: true });
    }
  }
}
