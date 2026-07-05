import { Component, Input, OnDestroy, OnInit, Signal, signal } from '@angular/core';
import { Appointment } from '../../../../models/appointment';
import { AsyncPipe, DatePipe, } from '@angular/common';
import { map, Observable, timer } from 'rxjs';

@Component({
  selector: 'appointment-card',
  imports: [DatePipe, AsyncPipe],
  templateUrl: './appointment-card.html',
  styleUrls: ['./appointment-card.css'],
})
export class AppointmentCard{
  /** The unique key for the appointment card */
  @Input() key!: string;
  /** The appointment to display in the card */
  @Input() appointment!: Appointment;

  /** The current date and time, used to determine if the appointment is ongoing or upcoming  */
  private currentDate$: Observable<Date> = timer(0, 1000).pipe(map(() => new Date()));

  /**
   * Checks if the appointment is ongoing based on the current date and time.
   * An appointment is considered ongoing if the current date and time is between the appointment's start and end dates.
   * This property is an observable that emits a boolean value indicating whether the appointment is ongoing or not.
   * It updates every minute to ensure that the status of the appointment is accurate.
   *
   * @returns {Observable<boolean>} An observable that emits true if the appointment is ongoing, false otherwise.
   */
  public isOngoing$ : Observable<boolean> = this.currentDate$.pipe(
    map(currentDate => this.overlap(currentDate, this.appointment.startDate, this.appointment.endDate))
  );
  /**
   * Checks if the appointment is upcoming based on the current date and time.
   * An appointment is considered upcoming if the current date and time is before the appointment's start date.
   * This property is an observable that emits a boolean value indicating whether the appointment is upcoming or not.
   * It updates every minute to ensure that the status of the appointment is accurate.
   *
   * @returns {Observable<boolean>} An observable that emits true if the appointment is upcoming, false otherwise.
   */
  public isUpcoming$ : Observable<boolean> = this.currentDate$.pipe(
    map(currentDate => currentDate < this.appointment.startDate)
  );

  public isPast$ : Observable<boolean> = this.currentDate$.pipe(
    map(currentDate => currentDate > this.appointment.endDate)
  );

  private overlap(currentDate: Date, startDate: Date, endDate: Date): boolean {
    return (startDate <= currentDate && currentDate <= endDate);
  }
}
