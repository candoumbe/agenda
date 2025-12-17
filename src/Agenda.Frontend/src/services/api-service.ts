import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Appointment } from '../models/appointment';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
  deps: [HttpClient]
})

/**
 * Service for interacting with the API.
 */
export class ApiService {
  constructor(public http : HttpClient) {
    this.http = http;
  }

  /** Gets all appointments from the API */
  public  getAppointments() : Observable<Appointment[]> {
    return  this.http.get<Appointment[]>('/api/appointments');
  }
}
