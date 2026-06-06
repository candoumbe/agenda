import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TopbarComponent } from './topbar.component';

describe('TopbarComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TopbarComponent],
      providers: [provideRouter([])]
    }).compileComponents();
  });

  it('should hide protected navigation and search when not authenticated', () => {
    const fixture = TestBed.createComponent(TopbarComponent);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('.topbar__menu')).toBeNull();
    expect(compiled.querySelector('.topbar__search-input')).toBeNull();
    expect(compiled.querySelector('.topbar__auth-button')).toBeTruthy();
  });

  it('should render the application menu and search input when authenticated', () => {
    const fixture = TestBed.createComponent(TopbarComponent);
    fixture.componentRef.setInput('isAuthenticated', true);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('.topbar__menu')).toBeTruthy();
    expect(compiled.querySelector('.topbar__search-input')).toBeTruthy();
  });

  it('should emit search changes and auth actions', () => {
    const fixture = TestBed.createComponent(TopbarComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    let emittedSearchTerm = '';
    let authActionTriggered = false;

    component.searchChange.subscribe((value) => {
      emittedSearchTerm = value;
    });

    component.authAction.subscribe(() => {
      authActionTriggered = true;
    });

    component.handleSearchInput({ target: { value: 'rendez-vous' } } as unknown as Event);
    component.handleAuthAction();

    expect(component.searchTerm()).toBe('rendez-vous');
    expect(emittedSearchTerm).toBe('rendez-vous');
    expect(authActionTriggered).toBe(true);
  });

  it('should show the account name when authenticated', () => {
    const fixture = TestBed.createComponent(TopbarComponent);
    fixture.componentRef.setInput('isAuthenticated', true);
    fixture.componentRef.setInput('accountName', 'Camille Dupont');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('.topbar__auth-label')?.textContent?.trim()).toBe('Camille Dupont');
  });

  it('should normalize the account name when authenticated', () => {
    const fixture = TestBed.createComponent(TopbarComponent);
    fixture.componentRef.setInput('isAuthenticated', true);
    fixture.componentRef.setInput('accountName', '   Camille   Dupont   ');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('.topbar__auth-label')?.textContent?.trim()).toBe('Camille Dupont');
  });
});
