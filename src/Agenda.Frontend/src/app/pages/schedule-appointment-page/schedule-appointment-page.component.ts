import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { Attendee } from '../../../models/attendee';
import { NewAppointmentPayload } from '../../../models/new-appointment-payload';
import { ApiService } from '../../../services/api-service';

@Component({
  selector: 'app-schedule-appointment-page',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './schedule-appointment-page.component.html',
  styleUrl: './schedule-appointment-page.component.css'
})
export class ScheduleAppointmentPageComponent {
  private readonly _formBuilder = inject(FormBuilder);
  private readonly _apiService = inject(ApiService);

  public isSubmitting = false;
  public createdAppointmentId: string | null = null;
  public failureMessage: string | null = null;

  public readonly appointmentForm = this._formBuilder.nonNullable.group({
    subject: ['', [Validators.required, Validators.maxLength(180)]],
    location: ['', [Validators.required, Validators.maxLength(180)]],
    startDate: ['', [Validators.required]],
    endDate: ['', [Validators.required]],
    attendees: this._formBuilder.array([this.createAttendeeForm()], [Validators.minLength(1)])
  });

  public get attendees() : FormArray {
    return this.appointmentForm.controls.attendees;
  }

  public addAttendee() : void {
    this.attendees.push(this.createAttendeeForm());
  }

  public removeAttendee(index: number) : void {
    if (this.attendees.length > 1) {
      this.attendees.removeAt(index);
    }
  }

  public submit() : void {
    this.failureMessage = null;
    this.createdAppointmentId = null;

    if (this.appointmentForm.invalid) {
      this.appointmentForm.markAllAsTouched();
      return;
    }

    const startDate = new Date(this.appointmentForm.controls.startDate.value);
    const endDate = new Date(this.appointmentForm.controls.endDate.value);

    if (endDate <= startDate) {
      this.failureMessage = 'La fin du rendez-vous doit être après le début.';
      return;
    }

    const attendees: Omit<Attendee, 'id'>[] = this.attendees.controls.map((attendeeControl) => {
      const group = attendeeControl as FormGroup;
      const name = (group.controls['name'].value as string).trim();
      const email = (group.controls['email'].value as string).trim();
      const phoneNumber = (group.controls['phoneNumber'].value as string).trim();

      return {
        name,
        email,
        phoneNumber: phoneNumber.length > 0 ? phoneNumber : null
      };
    });

    const payload: NewAppointmentPayload = {
      subject: this.appointmentForm.controls.subject.value.trim(),
      location: this.appointmentForm.controls.location.value.trim(),
      startDate: startDate.toISOString(),
      endDate: endDate.toISOString(),
      attendees
    };

    this.isSubmitting = true;

    this._apiService.scheduleAppointment(payload)
      .pipe(finalize(() => {
        this.isSubmitting = false;
      }))
      .subscribe({
        next: (result) => {
          this.createdAppointmentId = result.resource.id;
          this.resetForm();
        },
        error: () => {
          this.failureMessage = 'Impossible de planifier le rendez-vous pour le moment. Réessayez.';
        }
      });
  }

  private createAttendeeForm() : FormGroup {
    return this._formBuilder.nonNullable.group({
      name: ['', [Validators.required, Validators.maxLength(120)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(160)]],
      phoneNumber: ['', [Validators.maxLength(40)]]
    });
  }

  private resetForm() : void {
    this.appointmentForm.reset({
      subject: '',
      location: '',
      startDate: '',
      endDate: ''
    });

    while (this.attendees.length > 1) {
      this.attendees.removeAt(this.attendees.length - 1);
    }

    const firstAttendee = this.attendees.at(0) as FormGroup;
    firstAttendee.reset({
      name: '',
      email: '',
      phoneNumber: ''
    });
  }
}
