import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AppointmentTable } from './appointment-table';

describe('AppointmentTable', () => {
  let component: AppointmentTable;
  let fixture: ComponentFixture<AppointmentTable>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AppointmentTable]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AppointmentTable);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
    expect(component.appointments).toEqual([]);
  });
});
