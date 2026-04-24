import { TestBed } from '@angular/core/testing';

import { ApiService } from './api-service';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { NewAppointmentPayload } from '../models/new-appointment-payload';

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

  it('should schedule a new appointment with expected payload', () => {
    const payload: NewAppointmentPayload = {
      subject: 'Comite architecture',
      location: 'Salle Horizon',
      startDate: '2026-04-24T08:00:00.000Z',
      endDate: '2026-04-24T09:00:00.000Z',
      attendees: [
        {
          name: 'Aline Dupont',
          email: 'aline@example.fr',
          phoneNumber: '0600000000'
        }
      ]
    };

    apiService.scheduleAppointment(payload).subscribe((response) => {
      expect(response.resource.id).toBe('appt_001');
    });

    const req = httpMock.expectOne('/api/appointments');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(payload);

    req.flush({
      resource: {
        id: 'appt_001',
        subject: payload.subject,
        location: payload.location,
        startDate: payload.startDate,
        endDate: payload.endDate,
        attendees: payload.attendees
      },
      links: []
    });
  });

  it('should return an HTTP 400 error when appointment scheduling request is invalid', () => {
    const invalidAppointment: NewAppointmentPayload = {
      subject: '',
      location: '',
      startDate: '2026-04-24T10:00:00.000Z',
      endDate: '2026-04-24T09:00:00.000Z',
      attendees: []
    };

    apiService.scheduleAppointment(invalidAppointment).subscribe({
      next: () => { throw new Error('Expected HTTP 400 error'); },
      error: (err) => {
        expect(err.status).toBe(400);
        expect(err.statusText).toBe('Bad Request');
      }
    });

    const req = httpMock.expectOne('/api/appointments');
    expect(req.request.method).toBe('POST');
    req.flush({ message: 'Validation error' }, { status: 400, statusText: 'Bad Request' });
  });

    afterEach(() => {
      httpMock.verify();
    });
});
