import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { RouterTestingHarness } from '@angular/router/testing';
import { vi } from 'vitest';
import { routes } from './app.routes';
import { AuthService } from './auth/auth.service';

class AuthServiceStub {
  public readonly isAuthenticated = signal(false);
  public readonly userName = signal('Mon compte');
  public readonly login = vi.fn();
  public readonly logout = vi.fn();
}

describe('App routes auth behavior', () => {
  let authServiceStub: AuthServiceStub;

  beforeEach(async () => {
    authServiceStub = new AuthServiceStub();

    await TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        {
          provide: AuthService,
          useValue: authServiceStub
        }
      ]
    }).compileComponents();
  });

  it('should redirect to /login when navigating to a protected route while unauthenticated', async () => {
    const router = TestBed.inject(Router);
    authServiceStub.isAuthenticated.set(false);

    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/appointments');

    expect(router.url).toBe('/login?redirectTo=%2Fappointments');
  });

  it('should redirect to / when navigating to /login while authenticated', async () => {
    const router = TestBed.inject(Router);
    authServiceStub.isAuthenticated.set(true);

    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/login');

    expect(router.url).toBe('/');
  });
});
