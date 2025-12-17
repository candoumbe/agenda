import { Component, input, signal } from '@angular/core';
import { Appointment } from '../../models/appointment';
import { HttpClient } from '@angular/common/http';
import { ApiService } from '../../services/api-service';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'appointment-table',
  templateUrl: './appointment-table.html',
  styleUrl: './appointment-table.css',
  imports: [DatePipe]
  
})
export class AppointmentTable {
  /**
   * The appointments to display.
   * @type Array<Appointment>
   * @memberof AppointmentTable
   */
  appointments: Array<Appointment> = [];

  
  constructor(public readonly apiService: ApiService) {

  }
}
