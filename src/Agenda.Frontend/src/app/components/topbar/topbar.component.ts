import { Component, computed, input, output, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

interface TopbarMenuItem {
  label: string;
  path: string;
  exact: boolean;
}

@Component({
  selector: 'app-topbar',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './topbar.component.html',
  styleUrl: './topbar.component.css'
})
export class TopbarComponent {
  public readonly isAuthenticated = input(false);
  public readonly accountName = input('Mon compte');
  public readonly searchPlaceholder = input('Rechercher tout type de données');

  public readonly authAction = output<void>();
  public readonly searchChange = output<string>();

  public readonly searchTerm = signal('');

  public readonly menuItems: TopbarMenuItem[] = [
    {
      label: 'Accueil',
      path: '/',
      exact: true
    },
    {
      label: 'Agenda',
      path: '/appointments',
      exact: false
    },
    {
      label: 'Nouveau rendez-vous',
      path: '/appointments/new',
      exact: false
    },
    {
      label: 'Participants',
      path: '/attendees',
      exact: false
    }
  ];

  public readonly authLabel = computed(() => this.isAuthenticated() ? this.accountName() : 'Se connecter');
  public readonly authHint = computed(() => this.isAuthenticated() ? 'Compte' : 'Connexion');

  public handleSearchInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.searchTerm.set(target.value);
    this.searchChange.emit(this.searchTerm());
  }

  public handleAuthAction(): void {
    this.authAction.emit();
  }
}
