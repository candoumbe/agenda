import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-attendees-search-page',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './attendees-search-page.component.html',
  styleUrl: './attendees-search-page.component.css'
})
export class AttendeesSearchPageComponent {
  private readonly _router = inject(Router);
  private readonly _formBuilder = inject(FormBuilder);

  public readonly searchForm = this._formBuilder.nonNullable.group({
    name: [''],
    email: ['']
  });

  public goToHome(): void {
    this._router.navigate(['/']);
  }

  public searchAttendees(): void {
    // Stub: fonctionnalité à venir
  }
}
