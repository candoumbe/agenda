import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AppointmentCard } from './appointment-card';
import { Appointment } from '../../../../models/appointment';

describe('AppointmentCard', () => {
  let component: AppointmentCard;
  let fixture: ComponentFixture<AppointmentCard>;
  let appointment: Appointment;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppointmentCard],
    }).compileComponents();

    fixture = TestBed.createComponent(AppointmentCard);
    appointment = {
      id: '1',
      subject: 'Test Appointment',
      location: 'Test Location',
      startDate: new Date(Date.now() - 1000 * 60 * 30), // 30 minutes ago
      endDate: new Date(Date.now() + 1000 * 60 * 30), // 30 minutes from now
      attendees: [],
      createdAt: new Date(),
      createdBy: 'Bob',
    } as Appointment; // Cast to any to bypass type checking for this test

    component = fixture.componentInstance;
    component.key = appointment.id;
    component.appointment = appointment;

    await fixture.whenStable();
  });

  it('should create', () => {
    // Arrange
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

    component.key = appointment.id;
    component.appointment = appointment;
    fixture.detectChanges();

    // Assert
    expect(component).toBeTruthy();
  });

  it('should have an appointment input', () => {
    expect(component.appointment).toBeDefined();
  });

  it('should display ongoing badge when appointment is ongoing', async () => {

    // Arrange
    component.appointment.startDate = new Date(Date.now() - 1000 * 60 * 30); // 30 minutes ago
    component.appointment.endDate = new Date(Date.now() + 1000 * 60 * 30); // 30 minutes from now
    fixture.componentRef.setInput('appointment', component.appointment);

    await fixture.whenStable();

    // Assert
    const onGoingBadge = fixture.nativeElement.querySelector('.ongoing-badge');
    expect(onGoingBadge).toBeTruthy();
    expect(onGoingBadge.textContent).toContain('En cours');
  });

  it('should display upcoming badge when appointment is upcoming', async () => {
    // Arrange
    component.appointment.startDate = new Date(Date.now() + 1000 * 60 * 30); // 30 minutes from now
    component.appointment.endDate = new Date(Date.now() + 1000 * 60 * 60); // 1 hour from now
    fixture.componentRef.setInput('appointment', component.appointment);

    await fixture.whenStable();

    // Assert
    const upcomingBadge = fixture.nativeElement.querySelector('.upcoming-badge');
    expect(upcomingBadge).toBeTruthy();
    expect(upcomingBadge.textContent).toContain('À venir');
  });

  it('should display past badge when appointment is past', async () => {
    // Arrange
    component.appointment.startDate = new Date(Date.now() - 1000 * 60 * 60); // 1 hour ago
    component.appointment.endDate = new Date(Date.now() - 1000 * 60 * 30); // 30 minutes ago
    fixture.componentRef.setInput('appointment', component.appointment);

    await fixture.whenStable();

    // Assert
    const pastBadge = fixture.nativeElement.querySelector('.past-badge');
    expect(pastBadge).toBeTruthy();
    expect(pastBadge.textContent).toContain('Passé');
  });
});
