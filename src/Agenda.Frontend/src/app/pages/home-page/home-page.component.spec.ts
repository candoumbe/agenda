import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HomePageComponent } from './home-page.component';
import { Router } from '@angular/router';
import { vi } from 'vitest';

describe('HomePageComponent', () => {
  let component: HomePageComponent;
  let fixture: ComponentFixture<HomePageComponent>;
  let routerSpy: { navigate: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    routerSpy = {
      navigate: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [HomePageComponent],
      providers: [
        {
          provide: Router,
          useValue: routerSpy
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(HomePageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should navigate to /appointments when goToAppointments is called', () => {
    // Act
    component.goToAppointments();

    // Assert
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/appointments']);
  });

  it('should navigate to /appointments/new when goToNewAppointment is called', () => {
    // Act
    component.goToNewAppointment();

    // Assert
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/appointments/new']);
  });

  it('should navigate to /attendees when goToAttendees is called', () => {
    // Act
    component.goToAttendees();

    // Assert
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/attendees']);
  });

  it('should render the Agenda navigation card', () => {
    // Assert
    const cardTitles = fixture.nativeElement.querySelectorAll('.nav-card__title') as NodeList;
    const titles = Array.from(cardTitles).map((node) => (node as HTMLElement).textContent?.trim());
    expect(titles).toContain('Agenda');
  });

  it('should render the Nouveau rendez-vous navigation card', () => {
    // Assert
    const cardTitles = fixture.nativeElement.querySelectorAll('.nav-card__title') as NodeList;
    const titles = Array.from(cardTitles).map((node) => (node as HTMLElement).textContent?.trim());
    expect(titles).toContain('Nouveau rendez-vous');
  });

  it('should render the Participants navigation card', () => {
    // Assert
    const cardTitles = fixture.nativeElement.querySelectorAll('.nav-card__title') as NodeList;
    const titles = Array.from(cardTitles).map((node) => (node as HTMLElement).textContent?.trim());
    expect(titles).toContain('Participants');
  });

  it('should trigger goToAppointments when the Agenda card is clicked', () => {
    // Arrange
    const navigateSpy = vi.spyOn(component, 'goToAppointments');

    // Act
    const agendaCard = fixture.nativeElement.querySelector('.nav-card--primary') as HTMLButtonElement;
    agendaCard.click();

    // Assert
    expect(navigateSpy).toHaveBeenCalled();
  });

  it('should trigger goToNewAppointment when the accent card is clicked', () => {
    // Arrange
    const navigateSpy = vi.spyOn(component, 'goToNewAppointment');

    // Act
    const newCard = fixture.nativeElement.querySelector('.nav-card--accent') as HTMLButtonElement;
    newCard.click();

    // Assert
    expect(navigateSpy).toHaveBeenCalled();
  });

  it('should trigger goToAttendees when the tertiary card is clicked', () => {
    // Arrange
    const navigateSpy = vi.spyOn(component, 'goToAttendees');

    // Act
    const attendeesCard = fixture.nativeElement.querySelector('.nav-card--tertiary') as HTMLButtonElement;
    attendeesCard.click();

    // Assert
    expect(navigateSpy).toHaveBeenCalled();
  });
});
