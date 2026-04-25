import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Appointment } from '../models/appointment';
import { Observable } from 'rxjs';
import { NewAppointmentPayload } from '../models/new-appointment-payload';
import { Browsable } from '../models/browsable';
import { PageOf } from '../models/page-of';
import { SearchAppointmentsParams } from '../models/search-appointments-params';

@Injectable({
  providedIn: 'root',
  deps: [HttpClient]
})

/**
 * Service for interacting with the API.
 */
export class ApiService {
  constructor(public http: HttpClient) {
    this.http = http;
  }

  /** Gets paginated appointments from the API */
  public getAppointments(params?: SearchAppointmentsParams): Observable<PageOf<Browsable<Appointment>>> {
    let httpParams: HttpParams = new HttpParams();

    if (params) {
      if (params.page !== undefined) {
        httpParams = httpParams.set('page', params.page.toString());
      }
      if (params.pageSize !== undefined) {
        httpParams = httpParams.set('pageSize', params.pageSize.toString());
      }
      if (params.subject !== undefined && params.subject.trim()) {
        httpParams = httpParams.set('subject', params.subject);
      }
      if (params.from !== undefined) {
        httpParams = httpParams.set('from', params.from);
      }
      if (params.to !== undefined) {
        httpParams = httpParams.set('to', params.to);
      }
      if (params.sort !== undefined) {
        httpParams = httpParams.set('sort', params.sort);
      }
    }

    return this.http.get<PageOf<Browsable<Appointment>>>('/api/appointments', { params: httpParams });
  }

  /** Creates a new appointment. */
  public scheduleAppointment(payload: NewAppointmentPayload): Observable<Browsable<Appointment>> {
    return this.http.post<Browsable<Appointment>>('/api/appointments', payload);
  }
}
