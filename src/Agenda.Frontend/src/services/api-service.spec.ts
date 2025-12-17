import { TestBed } from '@angular/core/testing';

import { ApiService } from './api-service';
import {  HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

describe('ApiService with HTTP', () => {
  let apiService: ApiService;
  let httpMock : HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ApiService, provideHttpClientTesting()]
    });
    apiService = TestBed.inject(ApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  it('should be created', () => {
    expect(apiService).toBeTruthy();
  });

  it('should return an HTTP 500 error when the server fails', () => {
    apiService.getAppointments().subscribe({
      next: () => { throw new Error('Expected HTTP 500 error'); },
      error: (err) => {
        expect(err.status).toBe(500);
        expect(err.statusText).toBe('Internal Server Error');
      }
    });

    const req = httpMock.expectOne('/api/appointments');
    req.flush({ message: 'Server error' }, { status: 500, statusText: 'Internal Server Error' });
  });

  it('should return a network error when connection fails', () => {
    const emsg = 'simulated network error';

    return new Promise<void>((resolve) => {
      apiService.getAppointments().subscribe({
        next: () => { throw new Error('Expected network error'); },
        error: (err) => {
          expect(err.status).toBe(0);
          if (err.error && typeof err.error === 'object') {
            expect(err.error.message).toContain(emsg);
          } else if (typeof err.error === 'string') {
            expect(err.error).toBeTruthy();
          } else {
            throw new Error('Unexpected error shape');
          }
          resolve();
        }
      });

      const req = httpMock.expectOne('/api/appointments');
      req.error(new ErrorEvent('NetworkError', { message: emsg }));
    });
  });

    afterEach(() => {
      httpMock.verify();
    });
});
