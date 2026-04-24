import { Routes } from '@angular/router';
import { ScheduleAppointmentPageComponent } from './pages/schedule-appointment-page/schedule-appointment-page.component';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'appointments/new'
  },
  {
    path: 'appointments/new',
    component: ScheduleAppointmentPageComponent
  }
];
