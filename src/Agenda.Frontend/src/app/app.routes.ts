import { Routes } from '@angular/router';
import { ScheduleAppointmentPageComponent } from './pages/schedule-appointment-page/schedule-appointment-page.component';
import { AppointmentsListPageComponent } from './pages/appointments-list-page/appointments-list-page.component';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'appointments'
  },
  {
    path: 'appointments',
    component: AppointmentsListPageComponent
  },
  {
    path: 'appointments/new',
    component: ScheduleAppointmentPageComponent
  }
];
