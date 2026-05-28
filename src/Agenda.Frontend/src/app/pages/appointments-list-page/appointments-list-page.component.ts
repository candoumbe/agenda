import { CommonModule } from '@angular/common';
import { Component, DestroyRef, ChangeDetectorRef, ViewRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { debounceTime, distinctUntilChanged, finalize, map } from 'rxjs';
import { Appointment } from '../../../models/appointment';
import { ApiService } from '../../../services/api-service';
import { Browsable } from '../../../models/browsable';
import { SearchAppointmentsParams } from '../../../models/search-appointments-params';
import { PageOf } from '../../../models/page-of';

interface AppointmentGroup {
  date: string;
  appointments: Browsable<Appointment>[];
  isToday: boolean;
  isInThePast: boolean;
}

@Component({
  selector: 'app-appointments-list-page',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './appointments-list-page.component.html',
  styleUrl: './appointments-list-page.component.css'
})
export class AppointmentsListPageComponent implements OnInit {
  private static readonly DEFAULT_RANGE_IN_DAYS = 15;

  private readonly _formBuilder = inject(FormBuilder);
  private readonly _apiService = inject(ApiService);
  private readonly _router = inject(Router);
  private readonly _destroyRef = inject(DestroyRef);
  private readonly _changeDetectorRef = inject(ChangeDetectorRef);

  public isLoading = signal(false);
  public appointmentGroups = signal<AppointmentGroup[]>([]);
  public currentPage = signal(1);
  public totalPages = signal(1);
  public pageSize = signal(10);
  public hasPreviousPage = signal(false);
  public hasNextPage = signal(false);
  public hasError = signal(false);
  public errorMessage = signal('');
  public firstIncomingAppointmentOutsideInterval = signal<Browsable<Appointment> | null>(null);
  public totalResultsCount = signal<number | null>(null);
  public hasCountError = signal(false);

  public readonly searchForm = this._formBuilder.nonNullable.group({
    subject: [''],
    location: [''],
    fromDate: [''],
    fromTime: [''],
    toDate: [''],
    toTime: ['']
  });

  private newlyCreatedAppointmentId: string | null = null;
  private _previousPageFromLink: number | null = null;
  private _nextPageFromLink: number | null = null;

  ngOnInit(): void {
    this.newlyCreatedAppointmentId = this.extractNewlyCreatedId();
    this.applyDefaultDateInterval();

    this.searchForm.valueChanges
      .pipe(
        map(() => JSON.stringify(this.buildFilters())),
        debounceTime(300),
        distinctUntilChanged(),
        takeUntilDestroyed(this._destroyRef)
      )
      .subscribe(() => {
        this.loadAppointments(1);
      });

    this.loadAppointments();
  }

  public loadAppointments(page?: number): void {
    this.isLoading.set(true);
    this.hasError.set(false);
    this.errorMessage.set('');
    this.totalResultsCount.set(null);
    this.hasCountError.set(false);

    const targetPage = this.normalizePage(page ?? this.currentPage());
    const filters = this.buildFilters();

    const searchParams: SearchAppointmentsParams = {
      page: targetPage,
      pageSize: this.pageSize(),
      subject: filters.subject,
      location: filters.location,
      from: filters.from,
      to: filters.to
    };

    this._apiService.countAppointments(searchParams)
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe({
        next: (count) => {
          this.totalResultsCount.set(count);
        },
        error: () => {
          this.hasCountError.set(true);
        }
      });

    this._apiService.getAppointments(searchParams)
      .pipe(
        takeUntilDestroyed(this._destroyRef),
        finalize(() => {
          this.isLoading.set(false);

          const viewRef = this._changeDetectorRef as ViewRef;
          if (!viewRef.destroyed) {
            this._changeDetectorRef.detectChanges();
          }
        })
      )
      .subscribe({
        next: (response) => {
          this.applyPaginationState(response);
          this.appointmentGroups.set(this.groupAppointmentsByDate(response.items));
          this.firstIncomingAppointmentOutsideInterval.set(null);

          if (this.shouldSearchForIncomingAppointments(response, filters)) {
            this.loadFirstIncomingAppointment(filters);
          }
        },
        error: () => {
          this.hasError.set(true);
          this.errorMessage.set('Impossible de charger les rendez-vous. Réessayez.');
        }
      });
  }

  public searchAppointments(): void {
    this.loadAppointments(1);
  }

  public clearSearch(): void {
    this.searchForm.reset({
      subject: '',
      location: '',
      fromDate: '',
      fromTime: '',
      toDate: '',
      toTime: ''
    }, { emitEvent: false });
    this.applyDefaultDateInterval();
    this.loadAppointments(1);
  }

  public noAppointmentsMessage(): string {
    const fromDate = this.searchForm.controls.fromDate.value;
    const fromTime = this.searchForm.controls.fromTime.value;
    const toDate = this.searchForm.controls.toDate.value;
    const toTime = this.searchForm.controls.toTime.value;

    const from = this.formatBoundaryForDisplay(fromDate, fromTime, false);
    const to = this.formatBoundaryForDisplay(toDate, toTime, true);

    if (from && to) {
      return `No appointments between ${from} and ${to}`;
    }

    return 'Aucun rendez-vous trouvé.';
  }

  public canJumpToFirstIncomingAppointment(): boolean {
    return this.firstIncomingAppointmentOutsideInterval() !== null;
  }

  public jumpToFirstIncomingAppointment(): void {
    const firstIncomingAppointment = this.firstIncomingAppointmentOutsideInterval();
    if (!firstIncomingAppointment) {
      return;
    }

    const startDate = new Date(firstIncomingAppointment.resource.startDate);
    const endDate = this.addDays(startDate, AppointmentsListPageComponent.DEFAULT_RANGE_IN_DAYS);

    this.searchForm.patchValue(
      {
        fromDate: this.toDateValue(startDate),
        fromTime: this.toTimeValue(startDate),
        toDate: this.toDateValue(endDate),
        toTime: this.toTimeValue(endDate)
      },
      { emitEvent: false }
    );

    this.loadAppointments(1);
  }

  public nextPage(): void {
    if (this.hasNextPage() || this.currentPage() < this.totalPages()) {
      const nextPage = this._nextPageFromLink ?? this.currentPage() + 1;
      this.loadAppointments(nextPage);
    }
  }

  public previousPage(): void {
    if (this.hasPreviousPage() || this.currentPage() > 1) {
      const previousPage = this._previousPageFromLink ?? this.currentPage() - 1;
      this.loadAppointments(previousPage);
    }
  }

  public goToAppointmentCreation(): void {
    this._router.navigate(['/appointments/new']);
  }

  public isAppointmentOngoing(appointment: Appointment): boolean {
    const now = new Date();
    const startDate = new Date(appointment.startDate);
    const endDate = new Date(appointment.endDate);
    return startDate <= now && now < endDate;
  }

  public isAppointmentUpcoming(appointment: Appointment): boolean {
    const now = new Date();
    const startDate = new Date(appointment.startDate);
    return startDate > now;
  }

  public canGoToPreviousPage(): boolean {
    return this.hasPreviousPage() || this.currentPage() > 1;
  }

  public canGoToNextPage(): boolean {
    return this.hasNextPage() || this.currentPage() < this.totalPages();
  }

  private buildFilters(): Pick<SearchAppointmentsParams, 'subject' | 'location' | 'from' | 'to'> {
    const subject = this.searchForm.controls.subject.value.trim();
    const location = this.searchForm.controls.location.value.trim();
    const from = this.composeBoundaryIso(
      this.searchForm.controls.fromDate.value,
      this.searchForm.controls.fromTime.value,
      false
    );
    const to = this.composeBoundaryIso(
      this.searchForm.controls.toDate.value,
      this.searchForm.controls.toTime.value,
      true
    );

    return {
      subject: subject || undefined,
      location: location || undefined,
      from,
      to
    };
  }

  private composeBoundaryIso(dateValue: string, timeValue: string, isEndBoundary: boolean): string | undefined {
    const dateParts = this.parseDateValue(dateValue);
    if (!dateParts) {
      return undefined;
    }

    const resolvedTime = this.parseTimeValue(timeValue);
    const hours = resolvedTime ? resolvedTime.hours : isEndBoundary ? 23 : 0;
    const minutes = resolvedTime ? resolvedTime.minutes : isEndBoundary ? 59 : 0;
    const seconds = resolvedTime ? 0 : isEndBoundary ? 59 : 0;
    const milliseconds = resolvedTime ? 0 : isEndBoundary ? 999 : 0;

    const date = new Date(
      dateParts.year,
      dateParts.month - 1,
      dateParts.day,
      hours,
      minutes,
      seconds,
      milliseconds
    );

    return Number.isNaN(date.getTime()) ? undefined : date.toISOString();
  }

  private shouldSearchForIncomingAppointments(
    response: PageOf<Browsable<Appointment>>,
    filters: Pick<SearchAppointmentsParams, 'subject' | 'location' | 'from' | 'to'>
  ): boolean {
    return response.items.length === 0
      && response.count === 0
      && Boolean(filters.from)
      && Boolean(filters.to);
  }

  private loadFirstIncomingAppointment(filters: Pick<SearchAppointmentsParams, 'subject' | 'location' | 'from' | 'to'>): void {
    if (!filters.to) {
      this.firstIncomingAppointmentOutsideInterval.set(null);
      return;
    }

    const searchNextParams: SearchAppointmentsParams = {
      page: 1,
      pageSize: 1,
      subject: filters.subject,
      location: filters.location,
      from: filters.to
    };

    this._apiService.getAppointments(searchNextParams)
      .pipe(takeUntilDestroyed(this._destroyRef))
      .subscribe({
        next: (response) => {
          this.firstIncomingAppointmentOutsideInterval.set(response.items[0] ?? null);
        },
        error: () => {
          this.firstIncomingAppointmentOutsideInterval.set(null);
        }
      });
  }

  private applyDefaultDateInterval(): void {
    const fromDate = this.searchForm.controls.fromDate.value;
    const toDate = this.searchForm.controls.toDate.value;

    if (fromDate || toDate) {
      return;
    }

    const now = new Date();
    const end = this.addDays(now, AppointmentsListPageComponent.DEFAULT_RANGE_IN_DAYS);

    this.searchForm.patchValue(
      {
        fromDate: this.toDateValue(now),
        fromTime: '',
        toDate: this.toDateValue(end),
        toTime: ''
      },
      { emitEvent: false }
    );
  }

  private addDays(baseDate: Date, days: number): Date {
    const result = new Date(baseDate);
    result.setDate(result.getDate() + days);
    return result;
  }

  private toDateValue(date: Date): string {
    const year = date.getFullYear();
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const day = `${date.getDate()}`.padStart(2, '0');

    return `${year}-${month}-${day}`;
  }

  private toTimeValue(date: Date): string {
    const hours = `${date.getHours()}`.padStart(2, '0');
    const minutes = `${date.getMinutes()}`.padStart(2, '0');

    return `${hours}:${minutes}`;
  }

  private parseDateValue(value: string): { year: number; month: number; day: number } | null {
    if (!value) {
      return null;
    }

    const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value.trim());
    if (!match) {
      return null;
    }

    const year = Number.parseInt(match[1], 10);
    const month = Number.parseInt(match[2], 10);
    const day = Number.parseInt(match[3], 10);

    const parsedDate = new Date(year, month - 1, day, 12, 0, 0, 0);
    if (
      parsedDate.getFullYear() !== year
      || parsedDate.getMonth() !== month - 1
      || parsedDate.getDate() !== day
    ) {
      return null;
    }

    return { year, month, day };
  }

  private parseTimeValue(value: string): { hours: number; minutes: number } | null {
    if (!value) {
      return null;
    }

    const match = /^(\d{2}):(\d{2})$/.exec(value.trim());
    if (!match) {
      return null;
    }

    const hours = Number.parseInt(match[1], 10);
    const minutes = Number.parseInt(match[2], 10);

    if (hours < 0 || hours > 23 || minutes < 0 || minutes > 59) {
      return null;
    }

    return { hours, minutes };
  }

  private formatBoundaryForDisplay(dateValue: string, timeValue: string, isEndBoundary: boolean): string | null {
    const dateParts = this.parseDateValue(dateValue);
    if (!dateParts) {
      return null;
    }

    const resolvedTime = this.parseTimeValue(timeValue);
    const hours = resolvedTime ? resolvedTime.hours : isEndBoundary ? 23 : 0;
    const minutes = resolvedTime ? resolvedTime.minutes : isEndBoundary ? 59 : 0;

    const date = new Date(dateParts.year, dateParts.month - 1, dateParts.day, hours, minutes, 0, 0);

    return date.toLocaleString('fr-FR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  }

  private applyPaginationState(response: PageOf<Browsable<Appointment>>): void {
    const resolvedCurrentPage = this.normalizePage(response.page);
    const totalPagesFromLink = this.extractPageFromLink(response.links?.last?.href);
    const resolvedTotalPages = this.normalizePage(totalPagesFromLink ?? response.total);

    this.currentPage.set(Math.min(resolvedCurrentPage, resolvedTotalPages));
    this.totalPages.set(resolvedTotalPages);
    this.hasPreviousPage.set(Boolean(response.links?.previous));
    this.hasNextPage.set(Boolean(response.links?.next));
    this._previousPageFromLink = this.extractPageFromLink(response.links?.previous?.href);
    this._nextPageFromLink = this.extractPageFromLink(response.links?.next?.href);
  }

  private extractPageFromLink(href?: string): number | null {
    if (!href) {
      return null;
    }

    try {
      const url = new URL(href, window.location.origin);
      const page = Number.parseInt(url.searchParams.get('page') ?? '', 10);
      return Number.isNaN(page) ? null : this.normalizePage(page);
    } catch {
      return null;
    }
  }

  private normalizePage(page: number): number {
    return Number.isFinite(page) && page > 0 ? Math.floor(page) : 1;
  }

  private groupAppointmentsByDate(items: Browsable<Appointment>[]): AppointmentGroup[] {
    const grouped = new Map<string, Browsable<Appointment>[]>();
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    items.forEach(item => {
      const appointmentDate = new Date(item.resource.startDate);
      appointmentDate.setHours(0, 0, 0, 0);
      const dateKey = appointmentDate.toISOString().split('T')[0];

      if (!grouped.has(dateKey)) {
        grouped.set(dateKey, []);
      }
      grouped.get(dateKey)!.push(item);
    });

    return Array.from(grouped.entries())
      .sort(([dateA], [dateB]) => dateA.localeCompare(dateB))
      .map(([date, appointments]) => {
        const dateObj = new Date(date);
        const isToday = dateObj.getTime() === today.getTime();
        const isInThePast = dateObj.getTime() < today.getTime();

        return {
          date,
          appointments,
          isToday,
          isInThePast
        };
      });
  }

  private extractNewlyCreatedId(): string | null {
    const params = new URLSearchParams(window.location.search);
    return params.get('newlyCreatedId');
  }
}
