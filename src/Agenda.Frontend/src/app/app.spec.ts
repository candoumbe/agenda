import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { Router, provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { App } from './app';
import { AuthService } from './auth/auth.service';

class AuthServiceStub {
  public readonly isAuthenticated = signal(false);
  public readonly userName = signal('Mon compte');
  public readonly login = vi.fn();
  public readonly logout = vi.fn(() => {
    this.isAuthenticated.set(false);
  });
}

describe('App', () => {
  let authServiceStub: AuthServiceStub;

  beforeEach(async () => {
    authServiceStub = new AuthServiceStub();

    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: authServiceStub
        }
      ]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render router outlet', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('app-topbar')).toBeNull();
    expect(compiled.querySelector('router-outlet')).toBeTruthy();
  });
  
  it('should display the authenticated username in topbar', () => {
    authServiceStub.isAuthenticated.set(true);
    authServiceStub.userName.set('Camille Dupont');

    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.topbar__auth-label')?.textContent?.trim()).toBe('Camille Dupont');
  });

  it('should redirect to login when logout action is triggered from topbar', () => {
    authServiceStub.isAuthenticated.set(true);
    authServiceStub.userName.set('Camille Dupont');

    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate');

    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const authButton = fixture.nativeElement.querySelector('.topbar__auth-button') as HTMLButtonElement;
    authButton.click();

    expect(authServiceStub.logout).toHaveBeenCalledOnce();
    expect(navigateSpy).toHaveBeenCalledWith(['/login'], { replaceUrl: true });
  });
});
