import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { AppointmentsListPageComponent } from './appointments-list-page.component';
import { ApiService } from '../../../services/api-service';
import { Router } from '@angular/router';
import { defer, of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { Browsable } from '../../../models/browsable';
import { Appointment } from '../../../models/appointment';
import { PageOf } from '../../../models/page-of';
import { LOCALE_ID } from '@angular/core';
import localeFr from '@angular/common/locales/fr';
import { registerLocaleData } from '@angular/common';

registerLocaleData(localeFr);

describe('AppointmentsListPageComponent', () => {
  let component: AppointmentsListPageComponent;
  let fixture: ComponentFixture<AppointmentsListPageComponent>;
  let apiServiceSpy: { getAppointments: ReturnType<typeof vi.fn> };
  let routerSpy: { navigate: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    apiServiceSpy = {
      getAppointments: vi.fn().mockReturnValue(of({
        page: 1,
        total: 1,
        count: 1,
        items: [],
        links: {}
      }))
    };

    routerSpy = {
      navigate: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [AppointmentsListPageComponent],
      providers: [
        {
          provide: ApiService,
          useValue: apiServiceSpy
        },
        provideHttpClientTesting(),
        {
          provide: Router,
          useValue: routerSpy
        },
        { provide: LOCALE_ID, useValue: 'fr-FR' }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AppointmentsListPageComponent);
    component = fixture.componentInstance;
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load appointments on init', () => {
    const mockResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 1,
      count: 1,
      items: [
        {
          resource: {
            id: 'appt_001',
            subject: 'Team meeting',
            location: 'Conference room',
            startDate: '2026-04-25T09:00:00Z',
            endDate: '2026-04-25T10:00:00Z',
            attendees: []
          },
          links: []
        }
      ],
      links: {
        first: { href: '/appointments?page=1', relations: ['first'] },
        last: { href: '/appointments?page=1', relations: ['last'] }
      }
    };

    apiServiceSpy.getAppointments.mockReturnValue(of(mockResponse));

    fixture.detectChanges();

    expect(apiServiceSpy.getAppointments).toHaveBeenCalled();
    expect(component.appointmentGroups().length).toBeGreaterThan(0);
    expect(component.totalPages()).toBe(1);
  });

  it('should group appointments by date', () => {
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    const tomorrowStr = tomorrow.toISOString();

    const mockResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 1,
      count: 2,
      items: [
        {
          resource: {
            id: 'appt_001',
            subject: 'Meeting 1',
            location: 'Room A',
            startDate: tomorrowStr,
            endDate: new Date(tomorrow.getTime() + 3600000).toISOString(),
            attendees: []
          },
          links: []
        },
        {
          resource: {
            id: 'appt_002',
            subject: 'Meeting 2',
            location: 'Room B',
            startDate: tomorrowStr,
            endDate: new Date(tomorrow.getTime() + 7200000).toISOString(),
            attendees: []
          },
          links: []
        }
      ],
      links: {}
    };

    apiServiceSpy.getAppointments.mockReturnValue(of(mockResponse));

    fixture.detectChanges();

    const groups = component.appointmentGroups();
    expect(groups.length).toBe(1);
    expect(groups[0].appointments.length).toBe(2);
  });

  it('should mark today appointments', () => {
    const today = new Date();
    today.setHours(9, 0, 0, 0);
    const todayStr = today.toISOString();

    const mockResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 1,
      count: 1,
      items: [
        {
          resource: {
            id: 'appt_001',
            subject: 'Today meeting',
            location: 'Room A',
            startDate: todayStr,
            endDate: new Date(today.getTime() + 3600000).toISOString(),
            attendees: []
          },
          links: []
        }
      ],
      links: {}
    };

    apiServiceSpy.getAppointments.mockReturnValue(of(mockResponse));

    fixture.detectChanges();

    const groups = component.appointmentGroups();
    expect(groups[0].isToday).toBe(true);
  });

  it('should render appointment cards after async load without user interaction', async () => {
    const mockResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 1,
      count: 1,
      items: [
        {
          resource: {
            id: 'appt_009',
            subject: 'Async appointment',
            location: 'Room C',
            startDate: '2026-04-25T09:00:00Z',
            endDate: '2026-04-25T10:00:00Z',
            attendees: []
          },
          links: []
        }
      ],
      links: {}
    };

    apiServiceSpy.getAppointments.mockReturnValue(defer(() => Promise.resolve(mockResponse)));

    fixture.autoDetectChanges();
    await fixture.whenStable();

    const appointmentCards = fixture.nativeElement.querySelectorAll('.appointment-card');
    expect(appointmentCards.length).toBe(1);
  });

  it('should sync current page from API response and display it', () => {
    const mockResponse: PageOf<Browsable<Appointment>> = {
      page: 3,
      total: 99,
      count: 1,
      items: [
        {
          resource: {
            id: 'appt_010',
            subject: 'Paged appointment',
            location: 'Room D',
            startDate: '2026-04-25T11:00:00Z',
            endDate: '2026-04-25T12:00:00Z',
            attendees: []
          },
          links: []
        }
      ],
      links: {
        last: { href: '/appointments?page=5', relations: ['last'] }
      }
    };

    apiServiceSpy.getAppointments.mockReturnValue(of(mockResponse));

    fixture.detectChanges();

    expect(component.currentPage()).toBe(3);
    expect(component.totalPages()).toBe(5);

    const pageInfo = fixture.nativeElement.querySelector('.page-info') as HTMLElement;
    expect(pageInfo.textContent).toContain('Page 3 sur 5');
  });

  it('should identify ongoing appointments', () => {
    const now = new Date();
    const oneHourAgo = new Date(now.getTime() - 3600000);
    const oneHourFromNow = new Date(now.getTime() + 3600000);

    const mockResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 1,
      count: 1,
      items: [
        {
          resource: {
            id: 'appt_001',
            subject: 'Ongoing meeting',
            location: 'Room A',
            startDate: oneHourAgo.toISOString(),
            endDate: oneHourFromNow.toISOString(),
            attendees: []
          },
          links: []
        }
      ],
      links: {}
    };

    apiServiceSpy.getAppointments.mockReturnValue(of(mockResponse));

    fixture.detectChanges();

    const appointment = component.appointmentGroups()[0].appointments[0].resource;
    expect(component.isAppointmentOngoing(appointment)).toBe(true);
  });

  it('should handle API errors', () => {
    apiServiceSpy.getAppointments.mockReturnValue(throwError(() => new Error('API error')));

    fixture.detectChanges();

    expect(component.hasError()).toBe(true);
    expect(component.errorMessage()).toBeTruthy();
  });

  it('should request server-side search when subject changes after debounce', () => {
    vi.useFakeTimers();

    const mockResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 1,
      count: 1,
      items: [],
      links: {}
    };

    apiServiceSpy.getAppointments.mockReturnValue(of(mockResponse));

    fixture.detectChanges();
    expect(apiServiceSpy.getAppointments).toHaveBeenCalledTimes(1);

    component.currentPage.set(2);
    component.searchForm.controls.subject.setValue('Team meeting');

    vi.advanceTimersByTime(299);
    expect(apiServiceSpy.getAppointments).toHaveBeenCalledTimes(1);

    vi.advanceTimersByTime(1);

    expect(component.currentPage()).toBe(1);
    expect(apiServiceSpy.getAppointments).toHaveBeenCalledTimes(2);

    expect(apiServiceSpy.getAppointments).toHaveBeenLastCalledWith(
      expect.objectContaining({
        page: 1,
        pageSize: 10,
        subject: 'Team meeting',
        location: undefined,
        from: undefined,
        to: undefined
      })
    );
  });

  it('should request server-side search with multiple criteria', () => {
    const mockResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 1,
      count: 0,
      items: [],
      links: {}
    };

    apiServiceSpy.getAppointments.mockReturnValue(of(mockResponse));

    fixture.detectChanges();

    component.searchForm.patchValue({
      subject: 'Comite produit',
      location: 'Salle Neptune',
      from: '2026-05-02T09:30',
      to: '2026-05-02T10:30'
    }, { emitEvent: false });

    component.searchAppointments();

    expect(apiServiceSpy.getAppointments).toHaveBeenLastCalledWith(
      expect.objectContaining({
        page: 1,
        pageSize: 10,
        subject: 'Comite produit',
        location: 'Salle Neptune',
        from: new Date('2026-05-02T09:30').toISOString(),
        to: new Date('2026-05-02T10:30').toISOString()
      })
    );
  });

  it('should navigate to appointment creation', () => {
    component.goToAppointmentCreation();

    expect(routerSpy.navigate).toHaveBeenCalledWith(['/appointments/new']);
  });

  it('should handle pagination', () => {
    const firstPageResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 3,
      count: 10,
      items: [],
      links: {}
    };

    const secondPageResponse: PageOf<Browsable<Appointment>> = {
      page: 2,
      total: 3,
      count: 10,
      items: [],
      links: {}
    };

    apiServiceSpy.getAppointments
      .mockReturnValueOnce(of(firstPageResponse))
      .mockReturnValueOnce(of(secondPageResponse))
      .mockReturnValueOnce(of(firstPageResponse));

    fixture.detectChanges();
    expect(component.currentPage()).toBe(1);

    component.nextPage();
    expect(component.currentPage()).toBe(2);

    component.previousPage();
    expect(component.currentPage()).toBe(1);
  });

  it('should not go to next page when on last page', () => {
    const mockResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 1,
      count: 10,
      items: [],
      links: {}
    };

    apiServiceSpy.getAppointments.mockReturnValue(of(mockResponse));

    fixture.detectChanges();

    component.nextPage();
    expect(component.currentPage()).toBe(1);
  });

  it('should respect previous and next links to compute pagination boundaries', () => {
    const mockResponse: PageOf<Browsable<Appointment>> = {
      page: 2,
      total: 99,
      count: 10,
      items: [],
      links: {
        previous: { href: '/appointments?page=1', relations: ['previous'] },
        next: { href: '/appointments?page=3', relations: ['next'] },
        last: { href: '/appointments?page=3', relations: ['last'] }
      }
    };

    apiServiceSpy.getAppointments.mockReturnValue(of(mockResponse));

    fixture.detectChanges();

    expect(component.currentPage()).toBe(2);
    expect(component.totalPages()).toBe(3);
    expect(component.canGoToPreviousPage()).toBe(true);
    expect(component.canGoToNextPage()).toBe(true);
  });

  it('should clamp page display when API returns a page above last page', () => {
    const mockResponse: PageOf<Browsable<Appointment>> = {
      page: 9,
      total: 99,
      count: 0,
      items: [],
      links: {
        last: { href: '/appointments?page=3', relations: ['last'] }
      }
    };

    apiServiceSpy.getAppointments.mockReturnValue(of(mockResponse));

    fixture.detectChanges();

    expect(component.currentPage()).toBe(3);
    expect(component.totalPages()).toBe(3);
    expect(component.canGoToNextPage()).toBe(false);
  });

  it('should disable previous and next navigation for a single-page result', () => {
    const mockResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 1,
      count: 1,
      items: [
        {
          resource: {
            id: 'appt_011',
            subject: 'One item page',
            location: 'Room A',
            startDate: '2026-04-25T09:00:00Z',
            endDate: '2026-04-25T10:00:00Z',
            attendees: []
          },
          links: []
        }
      ],
      links: {
        first: { href: '/appointments?page=1', relations: ['first'] },
        last: { href: '/appointments?page=1', relations: ['last'] }
      }
    };

    apiServiceSpy.getAppointments.mockReturnValue(of(mockResponse));

    fixture.detectChanges();

    expect(component.canGoToPreviousPage()).toBe(false);
    expect(component.canGoToNextPage()).toBe(false);
  });

  it('should use next link target page when navigating forward', () => {
    const initialResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 99,
      count: 10,
      items: [],
      links: {
        next: { href: '/appointments?page=4', relations: ['next'] },
        last: { href: '/appointments?page=4', relations: ['last'] }
      }
    };

    const nextResponse: PageOf<Browsable<Appointment>> = {
      page: 4,
      total: 4,
      count: 0,
      items: [],
      links: {
        previous: { href: '/appointments?page=3', relations: ['previous'] },
        last: { href: '/appointments?page=4', relations: ['last'] }
      }
    };

    apiServiceSpy.getAppointments
      .mockReturnValueOnce(of(initialResponse))
      .mockReturnValueOnce(of(nextResponse));

    fixture.detectChanges();

    component.nextPage();

    expect(apiServiceSpy.getAppointments).toHaveBeenNthCalledWith(
      2,
      expect.objectContaining({
        page: 4
      })
    );
    expect(component.currentPage()).toBe(4);
  });

  it('should keep pagination stable for an empty result', () => {
    const mockResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 0,
      count: 0,
      items: [],
      links: {
        first: { href: '/appointments?page=1', relations: ['first'] },
        last: { href: '/appointments?page=1', relations: ['last'] }
      }
    };

    apiServiceSpy.getAppointments.mockReturnValue(of(mockResponse));

    fixture.detectChanges();

    expect(component.currentPage()).toBe(1);
    expect(component.totalPages()).toBe(1);
    expect(component.canGoToPreviousPage()).toBe(false);
    expect(component.canGoToNextPage()).toBe(false);
  });

  it('should not go to previous page when on first page', () => {
    const mockResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 2,
      count: 10,
      items: [],
      links: {}
    };

    apiServiceSpy.getAppointments.mockReturnValue(of(mockResponse));

    fixture.detectChanges();

    component.previousPage();
    expect(component.currentPage()).toBe(1);
  });

  it('should reset to page 1 when clearing search', () => {
    const mockResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 1,
      count: 10,
      items: [],
      links: {}
    };

    apiServiceSpy.getAppointments.mockReturnValue(of(mockResponse));

    component.searchForm.patchValue({
      subject: 'test',
      location: 'Room 7',
      from: '2026-05-02T09:30',
      to: '2026-05-02T10:30'
    }, { emitEvent: false });
    component.currentPage.set(2);
    component.clearSearch();

    expect(component.currentPage()).toBe(1);
    expect(component.searchForm.controls.subject.value).toBe('');
    expect(component.searchForm.controls.location.value).toBe('');
    expect(component.searchForm.controls.from.value).toBe('');
    expect(component.searchForm.controls.to.value).toBe('');
  });
});
