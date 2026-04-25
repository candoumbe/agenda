import { CommonModule } from '@angular/common';
import { Component, DestroyRef, ChangeDetectorRef, ViewRef, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { debounceTime, distinctUntilChanged, finalize, map } from 'rxjs';
import { Appointment } from '../../../models/appointment';
import { ApiService } from '../../../services/api-service';
import { Browsable } from '../../../models/browsable';

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
  public hasError = signal(false);
  public errorMessage = signal('');

  public readonly searchForm = this._formBuilder.nonNullable.group({
    subject: ['']
  });

  private newlyCreatedAppointmentId: string | null = null;

  ngOnInit(): void {
    this.newlyCreatedAppointmentId = this.extractNewlyCreatedId();

    this.searchForm.controls.subject.valueChanges
      .pipe(
        map((value) => value.trim()),
        debounceTime(300),
        distinctUntilChanged(),
        takeUntilDestroyed(this._destroyRef)
      )
      .subscribe(() => {
        this.currentPage.set(1);
        this.loadAppointments();
      });

    this.loadAppointments();
  }

  public loadAppointments(): void {
    this.isLoading.set(true);
    this.hasError.set(false);
    this.errorMessage.set('');

    const subject = this.searchForm.controls.subject.value?.trim();
    const searchParams = {
      page: this.currentPage(),
      pageSize: this.pageSize(),
      subject: subject || undefined
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
          this.currentPage.set(response.page);
          this.totalPages.set(response.total);
          this.appointmentGroups.set(this.groupAppointmentsByDate(response.items));
        },
        error: () => {
          this.hasError.set(true);
          this.errorMessage.set('Impossible de charger les rendez-vous. Réessayez.');
        }
      });
  }

  public searchAppointments(): void {
    this.currentPage.set(1);
    this.loadAppointments();
  }

  public clearSearch(): void {
    this.searchForm.controls.subject.reset('', { emitEvent: false });
    this.currentPage.set(1);
    this.loadAppointments();
  }

  public nextPage(): void {
    if (this.currentPage() < this.totalPages()) {
      this.currentPage.update(p => p + 1);
      this.loadAppointments();
    }
  }

  public previousPage(): void {
    if (this.currentPage() > 1) {
      this.currentPage.update(p => p - 1);
      this.loadAppointments();
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
