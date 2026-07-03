import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AppointmentCard } from './appointment-card';
import { Appointment } from '../../../../models/appointment';

describe('AppointmentCard', () => {
  let component: AppointmentCard;
  let fixture: ComponentFixture<AppointmentCard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppointmentCard],
    }).compileComponents();

    fixture = TestBed.createComponent(AppointmentCard);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have an appointment input', () => {
    expect(component.appointment).toBeUndefined();
  });

  it('should display ongoing badge when appointment is ongoing', () => {
    const appointment = {
      id: '1',
      subject: 'Test Appointment',
      location: 'Test Location',
      startDate: new Date(Date.now() - 1000 * 60 * 30), // 30 minutes ago
      endDate: new Date(Date.now() + 1000 * 60 * 30), // 30 minutes from now
      attendees: [],
      createdAt: new Date(),
      createdBy: 'Bob',
    } as Appointment; // Cast to any to bypass type checking for this test

    component.appointment = appointment;
    fixture.detectChanges();

    const badgeElement = fixture.nativeElement.querySelector('.ongoing-badge');
    expect(badgeElement).toBeTruthy();
    expect(badgeElement.textContent).toContain('En cours');
  });
});
