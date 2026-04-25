import { Component, input, signal } from '@angular/core';
import { Appointment } from '../../models/appointment';
import { HttpClient } from '@angular/common/http';
import { ApiService } from '../../services/api-service';
import { DatePipe } from '@angular/common';
import { Browsable } from '../../models/browsable';

@Component({
  selector: 'appointment-table',
  templateUrl: './appointment-table.html',
  styleUrl: './appointment-table.css',
  imports: [DatePipe]

})
export class AppointmentTable {
  /**
   * The appointments to display.
   * @type Array<Browsable<Appointment>>
   * @memberof AppointmentTable
   */
  appointments: Array<Browsable<Appointment>> = [];


  constructor(public readonly apiService: ApiService) {

  }
}
