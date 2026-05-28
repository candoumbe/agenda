import { TestBed } from '@angular/core/testing';

import { ApiService } from './api-service';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { NewAppointmentPayload } from '../models/new-appointment-payload';
import { PageOf } from '../models/page-of';
import { Browsable } from '../models/browsable';
import { Appointment } from '../models/appointment';

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

  it('should return paginated appointments with query parameters', () => {
    const mockResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 2,
      count: 1,
      items: [
        {
          resource: {
            id: 'appt_001',
            subject: 'Team meeting',
            location: 'Conference room',
            startDate: '2026-04-24T09:00:00Z',
            endDate: '2026-04-24T10:00:00Z',
            attendees: []
          },
          links: []
        }
      ],
      links: {
        first: { href: '/appointments?page=1', relations: ['first'] },
        last: { href: '/appointments?page=2', relations: ['last'] },
        next: { href: '/appointments?page=2', relations: ['next'] }
      }
    };

    apiService.getAppointments({ page: 1, pageSize: 10 }).subscribe((response) => {
      expect(response.page).toBe(1);
      expect(response.items.length).toBe(1);
      expect(response.items[0].resource.id).toBe('appt_001');
    });

    const req = httpMock.expectOne((request) => {
      return request.url === '/api/appointments' && request.params.get('page') === '1' && request.params.get('pageSize') === '10';
    });
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('should merge pagination metadata from HATEOAS headers when body links are missing', () => {
    const mockResponse: PageOf<Browsable<Appointment>> = {
      page: 2,
      total: 1,
      count: 0,
      items: [],
      links: {}
    };

    apiService.getAppointments({ page: 2, pageSize: 10 }).subscribe((response) => {
      expect(response.page).toBe(2);
      expect(response.total).toBe(4);
      expect(response.count).toBe(10);
      expect(response.links.previous?.href).toContain('page=1');
      expect(response.links.next?.href).toContain('page=3');
      expect(response.links.last?.href).toContain('page=4');
    });

    const req = httpMock.expectOne((request) =>
      request.url === '/api/appointments'
      && request.params.get('page') === '2'
      && request.params.get('pageSize') === '10'
    );

    req.flush(mockResponse, {
      headers: {
        total: '40',
        count: '10',
        Link: [
          '</api/appointments?page=1&pageSize=10>; rel="first"',
          '</api/appointments?page=1&pageSize=10>; rel="previous"',
          '</api/appointments?page=3&pageSize=10>; rel="next"',
          '</api/appointments?page=4&pageSize=10>; rel="last"'
        ]
      }
    });
  });

  it('should support rel="prev" alias from link headers', () => {
    const mockResponse: PageOf<Browsable<Appointment>> = {
      page: 2,
      total: 2,
      count: 1,
      items: [],
      links: {}
    };

    apiService.getAppointments({ page: 2, pageSize: 10 }).subscribe((response) => {
      expect(response.links.previous?.href).toContain('page=1');
    });

    const req = httpMock.expectOne((request) =>
      request.url === '/api/appointments'
      && request.params.get('page') === '2'
      && request.params.get('pageSize') === '10'
    );

    req.flush(mockResponse, {
      headers: {
        Link: '</api/appointments?page=1&pageSize=10>; rel="prev"'
      }
    });
  });

  it('should normalize PascalCase paginated payload to frontend camelCase contract', () => {
    apiService.getAppointments({ page: 1, pageSize: 10 }).subscribe((response) => {
      expect(response.page).toBe(1);
      expect(response.total).toBe(3);
      expect(response.count).toBe(1);
      expect(response.items.length).toBe(1);
      expect(response.items[0].resource.id).toBe('appt_pascal_001');
      expect(response.items[0].resource.subject).toBe('Pascal payload');
      expect(response.links.last?.href).toContain('page=3');
      expect(response.links.next?.href).toContain('page=2');
    });

    const req = httpMock.expectOne((request) =>
      request.url === '/api/appointments'
      && request.params.get('page') === '1'
      && request.params.get('pageSize') === '10'
    );

    req.flush({
      Page: 1,
      Total: 3,
      Count: 1,
      Items: [
        {
          Resource: {
            Id: 'appt_pascal_001',
            Subject: 'Pascal payload',
            Location: 'Room E',
            StartDate: '2026-05-30T09:00:00Z',
            EndDate: '2026-05-30T10:00:00Z',
            Attendees: []
          },
          Links: []
        }
      ],
      Links: {
        Next: { Href: '/appointments?page=2', Relations: ['next'] },
        Last: { Href: '/appointments?page=3', Relations: ['last'] }
      }
    });
  });

  it('should filter appointments by subject', () => {
    const mockResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 1,
      count: 1,
      items: [],
      links: {}
    };

    apiService.getAppointments({ subject: 'meeting' }).subscribe(() => {
      expect(true).toBe(true);
    });

    const req = httpMock.expectOne((request) => {
      return request.url === '/api/appointments' && request.params.get('subject') === 'meeting';
    });
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('should filter appointments by subject, location and time range', () => {
    const mockResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 1,
      count: 0,
      items: [],
      links: {}
    };

    apiService.getAppointments({
      subject: 'meeting',
      location: 'Salle Atlas',
      from: '2026-05-02T08:00:00.000Z',
      to: '2026-05-02T10:00:00.000Z'
    }).subscribe(() => {
      expect(true).toBe(true);
    });

    const req = httpMock.expectOne((request) => {
      return request.url === '/api/appointments'
        && request.params.get('subject') === 'meeting'
        && request.params.get('location') === 'Salle Atlas'
        && request.params.get('from') === '2026-05-02T08:00:00.000Z'
        && request.params.get('to') === '2026-05-02T10:00:00.000Z';
    });

    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
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

  it('should return total appointment count from HEAD response total header', () => {
    // countAppointments() will be implemented by Dallas — tests prepared ahead of time
    return new Promise<void>((resolve) => {
      (apiService as any).countAppointments().subscribe((count: number) => {
        expect(count).toBe(5);
        resolve();
      });

      const req = httpMock.expectOne('/api/appointments');
      expect(req.request.method).toBe('HEAD');
      req.flush(null, { headers: { total: '5' } });
    });
  });

  it('should return 0 when total header is absent from HEAD response', () => {
    return new Promise<void>((resolve) => {
      (apiService as any).countAppointments().subscribe((count: number) => {
        expect(count).toBe(0);
        resolve();
      });

      const req = httpMock.expectOne('/api/appointments');
      expect(req.request.method).toBe('HEAD');
      req.flush(null, { headers: {} });
    });
  });

    afterEach(() => {
      httpMock.verify();
    });
});
