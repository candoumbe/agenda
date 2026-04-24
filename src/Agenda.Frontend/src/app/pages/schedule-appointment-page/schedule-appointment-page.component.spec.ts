import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { ApiService } from '../../../services/api-service';
import { ScheduleAppointmentPageComponent } from './schedule-appointment-page.component';

describe('ScheduleAppointmentPageComponent', () => {
  let component: ScheduleAppointmentPageComponent;
  let fixture: ComponentFixture<ScheduleAppointmentPageComponent>;
  let apiServiceSpy: { scheduleAppointment: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    apiServiceSpy = {
      scheduleAppointment: vi.fn().mockReturnValue(of({
        resource: {
          id: 'appt_001',
          subject: 'Point demo',
          location: 'Salle Atlas',
          startDate: '2026-04-24T08:00:00.000Z',
          endDate: '2026-04-24T09:00:00.000Z',
          attendees: []
        },
        links: []
      }))
    };

    await TestBed.configureTestingModule({
      imports: [ScheduleAppointmentPageComponent],
      providers: [
        {
          provide: ApiService,
          useValue: apiServiceSpy
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ScheduleAppointmentPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('does not submit when form is invalid', () => {
    component.submit();

    expect(apiServiceSpy.scheduleAppointment).not.toHaveBeenCalled();
  });

  it('submits valid payload and displays success feedback', () => {
    component.appointmentForm.controls.subject.setValue('Comite produit');
    component.appointmentForm.controls.location.setValue('Visio');
    component.appointmentForm.controls.startDate.setValue('2026-04-24T08:00');
    component.appointmentForm.controls.endDate.setValue('2026-04-24T09:00');

    const firstAttendee = component.attendees.at(0);
    firstAttendee.get('name')?.setValue('Alice Martin');
    firstAttendee.get('email')?.setValue('alice@example.fr');
    firstAttendee.get('phoneNumber')?.setValue('0601020304');

    component.submit();

    expect(apiServiceSpy.scheduleAppointment).toHaveBeenCalledTimes(1);
    expect(apiServiceSpy.scheduleAppointment).toHaveBeenCalledWith(expect.objectContaining({
      subject: 'Comite produit',
      location: 'Visio',
      attendees: [
        {
          name: 'Alice Martin',
          email: 'alice@example.fr',
          phoneNumber: '0601020304'
        }
      ]
    }));

    expect(component.createdAppointmentId).toBe('appt_001');
  });
});
