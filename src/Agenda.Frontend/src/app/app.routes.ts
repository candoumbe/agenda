import { Routes } from '@angular/router';
import { ScheduleAppointmentPageComponent } from './pages/schedule-appointment-page/schedule-appointment-page.component';
import { AppointmentsListPageComponent } from './pages/appointments-list-page/appointments-list-page.component';
import { HomePageComponent } from './pages/home-page/home-page.component';
import { AttendeesSearchPageComponent } from './pages/attendees-search-page/attendees-search-page.component';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    component: HomePageComponent
  },
  {
    path: 'appointments',
    component: AppointmentsListPageComponent
  },
  {
    path: 'appointments/new',
    component: ScheduleAppointmentPageComponent
  },
  {
    path: 'attendees',
    component: AttendeesSearchPageComponent
  }
];
