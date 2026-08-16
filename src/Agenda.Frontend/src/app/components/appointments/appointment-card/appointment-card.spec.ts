import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AppointmentCard } from './appointment-card';
import { Appointment } from '../../../../models/appointment';

// Deterministically flushes the macrotask queue so timer(0, 1000) emits its first value, without depending on NgZone stability tracking of a never-completing periodic timer
const flushTimer = () => new Promise<void>((resolve) => setTimeout(resolve, 0));

describe('AppointmentCard', () => {
  let component: AppointmentCard;
  let fixture: ComponentFixture<AppointmentCard>;
  let appointment: Appointment;
  let now: Date;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppointmentCard],
    }).compileComponents();

    now = new Date();
    fixture = TestBed.createComponent(AppointmentCard);
    appointment = {
      id: '1',
      subject: 'Test Appointment',
      location: 'Test Location',
      startDate: new Date(now.getTime() - 1000 * 60 * 30), // 30 minutes ago
      endDate: new Date(now.getTime() + 1000 * 60 * 30), // 30 minutes from now
      attendees: [],
      createdAt: now,
      createdBy: 'Bob',
    } as Appointment; // Cast to any to bypass type checking for this test

    component = fixture.componentInstance;
    component.key = appointment.id;
    component.appointment = appointment;
  });

  afterEach(() => {
    // Ensures the timer(0, 1000) subscription in the component is torn down so it can't leak into the next test
    fixture.destroy();
  });

  it('should create', async () => {
    fixture.detectChanges();
    await flushTimer();

    // Assert
    expect(component).toBeTruthy();
  });

  it('should have an appointment input', () => {
    expect(component.appointment).toBeDefined();
  });

  it('should display ongoing badge when appointment is ongoing', async () => {
    // Arrange
    component.appointment.startDate = new Date(now.getTime() - 1000 * 60 * 30); // 30 minutes ago
    component.appointment.endDate = new Date(now.getTime() + 1000 * 60 * 30); // 30 minutes from now
    fixture.componentRef.setInput('appointment', component.appointment);
    fixture.detectChanges();
    await flushTimer();
    fixture.detectChanges();

    // Assert
    const onGoingBadge = fixture.nativeElement.querySelector('.ongoing-badge');
    expect(onGoingBadge).toBeTruthy();
    expect(onGoingBadge.textContent).toContain('En cours');
  });

  it('should display upcoming badge when appointment is upcoming', async () => {
    // Arrange
    component.appointment.startDate = new Date(now.getTime() + 1000 * 60 * 30); // 30 minutes from now
    component.appointment.endDate = new Date(now.getTime() + 1000 * 60 * 60); // 1 hour from now
    fixture.componentRef.setInput('appointment', component.appointment);
    fixture.detectChanges();
    await flushTimer();
    fixture.detectChanges();

    // Assert
    const upcomingBadge = fixture.nativeElement.querySelector('.upcoming-badge');
    expect(upcomingBadge).toBeTruthy();
    expect(upcomingBadge.textContent).toContain('À venir');
  });

  it('should display past badge when appointment is past', async () => {
    // Arrange
    component.appointment.startDate = new Date(now.getTime() - 1000 * 60 * 60); // 1 hour ago
    component.appointment.endDate = new Date(now.getTime() - 1000 * 60 * 30); // 30 minutes ago
    fixture.componentRef.setInput('appointment', component.appointment);
    fixture.detectChanges();
    await flushTimer();
    fixture.detectChanges();

    // Assert
    const pastBadge = fixture.nativeElement.querySelector('.past-badge');
    expect(pastBadge).toBeTruthy();
    expect(pastBadge.textContent).toContain('Passé');
  });
});
