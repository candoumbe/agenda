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

function toDateValue(date: Date): string {
  const year = date.getFullYear();
  const month = `${date.getMonth() + 1}`.padStart(2, '0');
  const day = `${date.getDate()}`.padStart(2, '0');

  return `${year}-${month}-${day}`;
}

describe('AppointmentsListPageComponent', () => {
  let component: AppointmentsListPageComponent;
  let fixture: ComponentFixture<AppointmentsListPageComponent>;
  let apiServiceSpy: { getAppointments: ReturnType<typeof vi.fn>; countAppointments: ReturnType<typeof vi.fn> };
  let routerSpy: { navigate: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    apiServiceSpy = {
      getAppointments: vi.fn().mockReturnValue(of({
        page: 1,
        total: 1,
        count: 1,
        items: [],
        links: {}
      })),
      countAppointments: vi.fn().mockReturnValue(of(0))
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

  it('should initialize default date interval to [today, today + 15 days] on first load without requiring time', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-05-01T10:20:00Z'));

    const mockResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 1,
      count: 1,
      items: [
        {
          resource: {
            id: 'appt_default_range',
            subject: 'Within default range',
            location: 'Room A',
            startDate: new Date('2026-05-03T09:00:00Z'),
            endDate: new Date('2026-05-03T10:00:00Z'),
            attendees: [],
            createdAt: new Date('2026-05-01T10:20:00Z'),
            createdBy: 'user1',
            updatedAt: new Date('2026-05-01T10:20:00Z'),
            updatedBy: 'user1'
          },
          links: []
        }
      ],
      links: {}
    };

    apiServiceSpy.getAppointments.mockReturnValue(of(mockResponse));

    fixture.detectChanges();

    const expectedFromDate = toDateValue(new Date('2026-05-01T10:20:00Z'));
    const expectedToDate = toDateValue(new Date('2026-05-16T10:20:00Z'));

    expect(component.searchForm.controls.fromDate.value).toBe(expectedFromDate);
    expect(component.searchForm.controls.toDate.value).toBe(expectedToDate);
    expect(component.searchForm.controls.fromTime.value).toBe('');
    expect(component.searchForm.controls.toTime.value).toBe('');
    expect(apiServiceSpy.getAppointments).toHaveBeenCalledWith(
      expect.objectContaining({
        page: 1,
        pageSize: 10,
        from: new Date(2026, 4, 1, 0, 0, 0, 0).toISOString(),
        to: new Date(2026, 4, 16, 23, 59, 59, 999).toISOString()
      })
    );
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
            startDate: new Date('2026-04-25T09:00:00Z'),
            endDate: new Date('2026-04-25T10:00:00Z'),
            attendees: [],
            createdAt: new Date('2026-04-25T08:00:00Z'),
            createdBy: 'user1',
            updatedAt: new Date('2026-04-25T08:30:00Z'),
            updatedBy: 'user1'
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
            startDate: new Date(tomorrow.getTime()),
            endDate: new Date(tomorrow.getTime() + 3_600_000),
            attendees: [],
            createdAt: new Date(tomorrow.getTime() - 3_600_000),
            createdBy: 'user1',
            updatedAt: new Date(tomorrow.getTime() - 1_800_000),
            updatedBy: 'user1'
          },
          links: []
        },
        {
          resource: {
            id: 'appt_002',
            subject: 'Meeting 2',
            location: 'Room B',
            startDate: new Date(tomorrow.getTime() + 3_600_000),
            endDate: new Date(tomorrow.getTime() + 7_200_000),
            attendees: [],
            createdAt: new Date(tomorrow.getTime() - 3_600_000),
            createdBy: 'Alice',
            updatedAt: new Date(tomorrow.getTime() - 1_800_000),
            updatedBy: 'Bob'
          },
          links: []
        }
      ],
      links: {}
    };

    const distinctDates = new Set(mockResponse.items.map(item => item.resource.startDate.toString().substring(0, 10)));

    apiServiceSpy.getAppointments.mockReturnValue(of(mockResponse));

    fixture.detectChanges();
    // Act
    const groups = component.appointmentGroups();

    // Assert

    expect(groups.length).toBe(distinctDates.size);
    expect(groups[0].appointments.length).toBe(2);
  });

  it('should mark today appointments', () => {
    const today = new Date();
    today.setHours(9, 0, 0, 0);

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
            startDate: today,
            endDate: new Date(today.getTime() + 3600000),
            attendees: [],
            createdAt: new Date(today.getTime() - 3600000),
            createdBy: 'Alice'
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
            startDate: new Date('2026-04-25T09:00:00Z'),
            endDate: new Date('2026-04-25T10:00:00Z'),
            attendees: [],
            createdAt: new Date('2026-04-25T08:00:00Z'),
            createdBy: 'Alice'
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
            startDate: new Date('2026-04-25T11:00:00Z'),
            endDate: new Date('2026-04-25T12:00:00Z'),
            attendees: [],
            createdAt: new Date('2026-04-25T10:00:00Z'),
            createdBy: 'Alice'
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
    const createdDate = new Date(now.getTime() - 7200000);
    const updatedDate = new Date(now.getTime() - 1800000);
    const createdBy = 'Alice';
    const updatedBy = 'Bob';

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
            startDate: oneHourAgo,
            endDate: oneHourFromNow,
            attendees: [],
            createdAt: createdDate,
            createdBy: createdBy,
            updatedAt: updatedDate,
            updatedBy: updatedBy
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

  it('should show empty interval message and create button when no appointment exists in selected range', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-05-01T08:00:00Z'));

    const emptyResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 1,
      count: 0,
      items: [],
      links: {}
    };

    apiServiceSpy.getAppointments
      .mockReturnValueOnce(of(emptyResponse))
      .mockReturnValueOnce(of(emptyResponse));

    fixture.detectChanges();

    const emptyStateMessage = fixture.nativeElement.querySelector('.empty-state p') as HTMLElement;
    expect(emptyStateMessage.textContent).toContain('No appointments between');

    const createButtons = Array.from(fixture.nativeElement.querySelectorAll('.empty-state .primary')) as HTMLButtonElement[];
    expect(createButtons.length).toBe(1);
    expect(createButtons[0].textContent).toContain('+ Nouveau rendez-vous');
  });

  it('should show jump button when appointments exist after selected interval', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-05-01T08:00:00Z'));

    const emptyResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 1,
      count: 0,
      items: [],
      links: {}
    };

    const incomingResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 1,
      count: 1,
      items: [
        {
          resource: {
            id: 'appt_after_range',
            subject: 'After range',
            location: 'Room Z',
            startDate: new Date('2026-05-20T09:00:00Z'),
            endDate: new Date('2026-05-20T10:00:00Z'),
            attendees: [],
            createdAt: new Date('2026-05-20T08:00:00Z'),
            createdBy: 'Alice'
          },
          links: []
        }
      ],
      links: {}
    };

    apiServiceSpy.getAppointments
      .mockReturnValueOnce(of(emptyResponse))
      .mockReturnValueOnce(of(incomingResponse));

    fixture.detectChanges();

    const jumpButton = fixture.nativeElement.querySelector('.jump-button') as HTMLButtonElement | null;
    expect(jumpButton).not.toBeNull();
    expect(jumpButton?.textContent).toContain('Voir le premier rendez-vous à venir');
  });

  it('should update date range to [first incoming appointment, +15 days] when jump button is clicked', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-05-01T08:00:00Z'));

    const emptyResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 1,
      count: 0,
      items: [],
      links: {}
    };

    const firstIncomingStartDate = new Date('2026-05-20T09:00:00Z');
    const incomingResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 1,
      count: 1,
      items: [
        {
          resource: {
            id: 'appt_after_range_jump',
            subject: 'After range jump',
            location: 'Room Z',
            startDate: firstIncomingStartDate,
            endDate: new Date('2026-05-20T10:00:00Z'),
            attendees: [],
            createdAt: new Date('2026-05-20T08:00:00Z'),
            createdBy: 'Alice'
          },
          links: []
        }
      ],
      links: {}
    };

    const afterJumpResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 1,
      count: 1,
      items: incomingResponse.items,
      links: {}
    };

    apiServiceSpy.getAppointments
      .mockReturnValueOnce(of(emptyResponse))
      .mockReturnValueOnce(of(incomingResponse))
      .mockReturnValueOnce(of(afterJumpResponse));

    fixture.detectChanges();

    const jumpButton = fixture.nativeElement.querySelector('.jump-button') as HTMLButtonElement;
    jumpButton.click();

    const fromDateValue = component.searchForm.controls.fromDate.value;
    const fromTimeValue = component.searchForm.controls.fromTime.value;
    const toDateValue = component.searchForm.controls.toDate.value;
    const toTimeValue = component.searchForm.controls.toTime.value;

    expect(fromDateValue).toBe('2026-05-20');
    expect(fromTimeValue).toBe('09:00');
    expect(toDateValue).toBe('2026-06-04');
    expect(toTimeValue).toBe('09:00');

    const fromDate = new Date(`${fromDateValue}T${fromTimeValue}`);
    const toDate = new Date(`${toDateValue}T${toTimeValue}`);
    const daysDiff = Math.round((toDate.getTime() - fromDate.getTime()) / (1000 * 60 * 60 * 24));
    expect(daysDiff).toBe(15);

    expect(apiServiceSpy.getAppointments).toHaveBeenLastCalledWith(
      expect.objectContaining({
        page: 1,
        pageSize: 10,
        from: new Date('2026-05-20T09:00:00').toISOString(),
        to: new Date('2026-06-04T09:00:00').toISOString()
      })
    );
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
        from: expect.any(String),
        to: expect.any(String)
      })
    );
  });

  it('should request server-side search with multiple criteria', () => {
    const mockResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 1,
      count: 1,
      items: [],
      links: {}
    };

    apiServiceSpy.getAppointments.mockReturnValue(of(mockResponse));

    fixture.detectChanges();

    component.searchForm.patchValue({
      subject: 'Comite produit',
      location: 'Salle Neptune',
      fromDate: '2026-05-02',
      fromTime: '09:30',
      toDate: '2026-05-02',
      toTime: '10:30'
    }, { emitEvent: false });

    component.searchAppointments();

    expect(apiServiceSpy.getAppointments).toHaveBeenLastCalledWith(
      expect.objectContaining({
        page: 1,
        pageSize: 10,
        subject: 'Comite produit',
        location: 'Salle Neptune',
        from: new Date('2026-05-02T09:30:00').toISOString(),
        to: new Date('2026-05-02T10:30:00').toISOString()
      })
    );
  });

  it('should send start-of-day and end-of-day boundaries when only dates are provided', () => {
    const mockResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 1,
      count: 1,
      items: [],
      links: {}
    };

    apiServiceSpy.getAppointments.mockReturnValue(of(mockResponse));

    fixture.detectChanges();

    component.searchForm.patchValue({
      fromDate: '2026-05-02',
      fromTime: '',
      toDate: '2026-05-03',
      toTime: ''
    }, { emitEvent: false });

    component.searchAppointments();

    expect(apiServiceSpy.getAppointments).toHaveBeenLastCalledWith(
      expect.objectContaining({
        from: new Date(2026, 4, 2, 0, 0, 0, 0).toISOString(),
        to: new Date(2026, 4, 3, 23, 59, 59, 999).toISOString()
      })
    );
  });

  it('should initialize a default 15-day date interval and use it for the first load', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-05-01T08:15:00.000Z'));

    const mockResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 1,
      count: 0,
      items: [],
      links: {}
    };

    apiServiceSpy.getAppointments.mockReturnValue(of(mockResponse));

    fixture.detectChanges();

    const firstCallParams = apiServiceSpy.getAppointments.mock.calls[0][0];
    const fromDate = new Date(firstCallParams.from as string);
    const toDate = new Date(firstCallParams.to as string);

    expect(firstCallParams).toEqual(
      expect.objectContaining({
        from: expect.any(String),
        to: expect.any(String)
      })
    );
    expect(fromDate.getTime()).toBe(new Date(2026, 4, 1, 0, 0, 0, 0).getTime());
    expect(toDate.getTime() - fromDate.getTime()).toBe((16 * 24 * 60 * 60 * 1000) - 1);
  });

  it('should display an interval-based empty-state message and a creation CTA when no result exists in range', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-05-01T08:00:00.000Z'));

    const mockResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 1,
      count: 0,
      items: [],
      links: {
        first: { href: '/appointments?page=1', relations: ['first'] },
        last: { href: '/appointments?page=1', relations: ['last'] }
      }
    };

    apiServiceSpy.getAppointments.mockReturnValue(of(mockResponse));

    fixture.detectChanges();

    const emptyStateElement = fixture.nativeElement.querySelector('.empty-state') as HTMLElement;
    expect(emptyStateElement).toBeTruthy();
    expect(emptyStateElement.textContent).toContain('No appointments between');

    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    const createButton = buttons
      .find((button: HTMLButtonElement) => {
        const content = button.textContent?.toLowerCase() ?? '';
        return content.includes('nouveau') || content.includes('create');
      });

    expect(createButton).toBeTruthy();
  });

  it('should offer jump-to-first-incoming and update the filter window by 15 days', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-05-01T08:00:00.000Z'));

    const firstIncomingStart = new Date('2026-05-21T09:00:00.000Z');
    const expectedWindowEnd = new Date('2026-06-05T09:00:00.000Z');

    const emptyWindowResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 1,
      count: 0,
      items: [],
      links: {
        next: {
          href: `/appointments?from=${firstIncomingStart.toISOString()}`,
          relations: ['next']
        }
      }
    };

    const incomingWindowResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 1,
      count: 1,
      items: [
        {
          resource: {
            id: 'appt_541',
            subject: 'First incoming appointment',
            location: 'Room 541',
            startDate: firstIncomingStart,
            endDate: new Date('2026-05-21T10:00:00.000Z'),
            attendees: [],
            createdAt: new Date('2026-05-20T08:00:00.000Z'),
            createdBy: 'Alice'
          },
          links: []
        }
      ],
      links: {}
    };

    apiServiceSpy.getAppointments
      .mockReturnValueOnce(of(emptyWindowResponse))
      .mockReturnValueOnce(of(incomingWindowResponse));

    fixture.detectChanges();

    const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
    const jumpButton = buttons
      .find((button: HTMLButtonElement) => {
        const content = button.textContent?.toLowerCase() ?? '';
        return content.includes('premier rendez-vous à venir') || content.includes('first incoming') || content.includes('first appointment');
      });

    expect(jumpButton).toBeTruthy();

    jumpButton!.click();
    fixture.detectChanges();

    expect(apiServiceSpy.getAppointments).toHaveBeenNthCalledWith(
      3,
      expect.objectContaining({
        from: firstIncomingStart.toISOString(),
        to: expectedWindowEnd.toISOString()
      })
    );
    expect(component.searchForm.controls.fromDate.value).toContain('2026-05-21');
    expect(component.searchForm.controls.fromTime.value).toContain('09:00');
    expect(component.searchForm.controls.toDate.value).toContain('2026-06-05');
    expect(component.searchForm.controls.toTime.value).toContain('09:00');
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
            startDate: new Date('2026-04-25T09:00:00Z'),
            endDate: new Date('2026-04-25T10:00:00Z'),
            attendees: [],
            createdAt: new Date('2026-04-24T08:00:00Z'),
            createdBy: 'Alice',
            updatedAt: new Date('2026-04-24T09:00:00Z'),
            updatedBy: 'Bob'
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
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-05-01T08:00:00Z'));

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
      fromDate: '2026-05-02',
      fromTime: '09:30',
      toDate: '2026-05-02',
      toTime: '10:30'
    }, { emitEvent: false });
    component.currentPage.set(2);
    component.clearSearch();

    expect(component.currentPage()).toBe(1);
    expect(component.searchForm.controls.subject.value).toBe('');
    expect(component.searchForm.controls.location.value).toBe('');
    expect(component.searchForm.controls.fromDate.value).toBe(toDateValue(new Date('2026-05-01T08:00:00Z')));
    expect(component.searchForm.controls.toDate.value).toBe(toDateValue(new Date('2026-05-16T08:00:00Z')));
    expect(component.searchForm.controls.fromTime.value).toBe('');
    expect(component.searchForm.controls.toTime.value).toBe('');
  });

  it('should call countAppointments in parallel with getAppointments and expose the total count', () => {
    // Arrange
    const mockResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 1,
      count: 2,
      items: [],
      links: {}
    };

    apiServiceSpy.getAppointments.mockReturnValue(of(mockResponse));
    apiServiceSpy.countAppointments.mockReturnValue(of(42));

    // Act
    fixture.detectChanges();

    // Assert
    expect(apiServiceSpy.countAppointments).toHaveBeenCalledWith(
      expect.objectContaining({
        page: 1,
        pageSize: 10
      })
    );
    expect(component.totalResultsCount()).toBe(42);
    expect(component.hasCountError()).toBe(false);
  });

  it('should set hasCountError when countAppointments fails', () => {
    // Arrange
    const mockResponse: PageOf<Browsable<Appointment>> = {
      page: 1,
      total: 1,
      count: 0,
      items: [],
      links: {}
    };

    apiServiceSpy.getAppointments.mockReturnValue(of(mockResponse));
    apiServiceSpy.countAppointments.mockReturnValue(throwError(() => new Error('HEAD failed')));

    // Act
    fixture.detectChanges();

    // Assert
    expect(component.totalResultsCount()).toBeNull();
    expect(component.hasCountError()).toBe(true);
  });
});
