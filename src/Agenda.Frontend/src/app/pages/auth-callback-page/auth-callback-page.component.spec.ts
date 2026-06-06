import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { vi } from 'vitest';
import { AuthCallbackPageComponent } from './auth-callback-page.component';

describe('AuthCallbackPageComponent', () => {
  function configureComponent(queryParams: Record<string, string> = {}): ReturnType<typeof vi.fn> {
    const navigateByUrlSpy = vi.fn();

    TestBed.configureTestingModule({
      imports: [AuthCallbackPageComponent],
      providers: [
        {
          provide: Router,
          useValue: {
            navigateByUrl: navigateByUrlSpy
          }
        },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: convertToParamMap(queryParams)
            }
          }
        }
      ]
    });

    return navigateByUrlSpy;
  }

  beforeEach(() => {
    sessionStorage.removeItem('agenda.redirectTo');
  });

  it('should redirect to stored relative target after callback', () => {
    sessionStorage.setItem('agenda.redirectTo', '/appointments');
    const navigateByUrlSpy = configureComponent();

    const fixture = TestBed.createComponent(AuthCallbackPageComponent);
    fixture.detectChanges();

    expect(navigateByUrlSpy).toHaveBeenCalledWith('/appointments', { replaceUrl: true });
    expect(sessionStorage.getItem('agenda.redirectTo')).toBeNull();
  });

  it('should fallback to home when stored redirect is unsafe', () => {
    sessionStorage.setItem('agenda.redirectTo', '//evil.example/path');
    const navigateByUrlSpy = configureComponent();

    const fixture = TestBed.createComponent(AuthCallbackPageComponent);
    fixture.detectChanges();

    expect(navigateByUrlSpy).toHaveBeenCalledWith('/', { replaceUrl: true });
  });

  it('should honor redirectTo query parameter when provided', () => {
    const navigateByUrlSpy = configureComponent({ redirectTo: '/attendees' });

    const fixture = TestBed.createComponent(AuthCallbackPageComponent);
    fixture.detectChanges();

    expect(navigateByUrlSpy).toHaveBeenCalledWith('/attendees', { replaceUrl: true });
  });
});
