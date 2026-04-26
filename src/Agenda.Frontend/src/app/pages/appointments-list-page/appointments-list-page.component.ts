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

  public readonly searchForm = this._formBuilder.nonNullable.group({
    subject: [''],
    location: [''],
    from: [''],
    to: ['']
  });

  private newlyCreatedAppointmentId: string | null = null;
  private _previousPageFromLink: number | null = null;
  private _nextPageFromLink: number | null = null;

  ngOnInit(): void {
    this.newlyCreatedAppointmentId = this.extractNewlyCreatedId();

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
    this.searchForm.reset({ subject: '', location: '', from: '', to: '' }, { emitEvent: false });
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
    const from = this.toIsoOrUndefined(this.searchForm.controls.from.value);
    const to = this.toIsoOrUndefined(this.searchForm.controls.to.value);

    return {
      subject: subject || undefined,
      location: location || undefined,
      from,
      to
    };
  }

  private toIsoOrUndefined(value: string): string | undefined {
    if (!value) {
      return undefined;
    }

    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? undefined : date.toISOString();
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
