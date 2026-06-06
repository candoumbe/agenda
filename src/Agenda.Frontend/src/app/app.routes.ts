import { Routes } from '@angular/router';
import { ScheduleAppointmentPageComponent } from './pages/schedule-appointment-page/schedule-appointment-page.component';
import { AppointmentsListPageComponent } from './pages/appointments-list-page/appointments-list-page.component';
import { HomePageComponent } from './pages/home-page/home-page.component';
import { AttendeesSearchPageComponent } from './pages/attendees-search-page/attendees-search-page.component';
import { LoginPageComponent } from './pages/login-page/login-page.component';
import { AuthCallbackPageComponent } from './pages/auth-callback-page/auth-callback-page.component';
import { authGuard, loginPageGuard } from './auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    component: LoginPageComponent,
    canActivate: [loginPageGuard]
  },
  {
    path: 'auth/callback',
    component: AuthCallbackPageComponent
  },
  {
    path: '',
    pathMatch: 'full',
    component: HomePageComponent,
    canActivate: [authGuard]
  },
  {
    path: 'appointments',
    component: AppointmentsListPageComponent,
    canActivate: [authGuard]
  },
  {
    path: 'appointments/new',
    component: ScheduleAppointmentPageComponent,
    canActivate: [authGuard]
  },
  {
    path: 'attendees',
    component: AttendeesSearchPageComponent,
    canActivate: [authGuard]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
