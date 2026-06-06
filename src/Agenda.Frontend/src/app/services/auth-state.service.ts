import { Injectable, computed, signal } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class AuthStateService {
  private static readonly DEFAULT_USERNAME = 'Mon compte';
  private static readonly USERNAME_MAX_LENGTH = 80;

  private readonly _isAuthenticated = signal(false);
  private readonly _username = signal(AuthStateService.DEFAULT_USERNAME);

  public readonly isAuthenticated = computed(() => this._isAuthenticated());
  public readonly username = computed(() => this._username());

  public login(username: string): void {
    this._username.set(this.sanitizeUsername(username));
    this._isAuthenticated.set(true);
  }

  public logout(): void {
    this._isAuthenticated.set(false);
    this._username.set(AuthStateService.DEFAULT_USERNAME);
  }

  private sanitizeUsername(username: string): string {
    const withoutControlCharacters = username.replace(/[\u0000-\u001F\u007F]/g, '');
    const normalizedWhitespace = withoutControlCharacters.replace(/\s+/g, ' ').trim();

    if (!normalizedWhitespace) {
      return AuthStateService.DEFAULT_USERNAME;
    }

    return normalizedWhitespace.slice(0, AuthStateService.USERNAME_MAX_LENGTH);
  }
}
