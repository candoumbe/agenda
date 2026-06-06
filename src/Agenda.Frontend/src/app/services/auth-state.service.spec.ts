import { TestBed } from '@angular/core/testing';
import { AuthStateService } from './auth-state.service';

describe('AuthStateService', () => {
  let service: AuthStateService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(AuthStateService);
  });

  it('should authenticate and normalize username on login', () => {
    service.login('  Camille   Dupont  ');

    expect(service.isAuthenticated()).toBe(true);
    expect(service.username()).toBe('Camille Dupont');
  });

  it('should fallback to default username when login input is blank', () => {
    service.login('    ');

    expect(service.username()).toBe('Mon compte');
  });

  it('should reset session state on logout', () => {
    service.login('Camille');

    service.logout();

    expect(service.isAuthenticated()).toBe(false);
    expect(service.username()).toBe('Mon compte');
  });
});
