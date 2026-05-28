import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-home-page',
  imports: [],
  templateUrl: './home-page.component.html',
  styleUrl: './home-page.component.css'
})
export class HomePageComponent {
  private readonly _router = inject(Router);

  public goToAppointments(): void {
    this._router.navigate(['/appointments']);
  }

  public goToNewAppointment(): void {
    this._router.navigate(['/appointments/new']);
  }

  public goToAttendees(): void {
    this._router.navigate(['/attendees']);
  }
}
