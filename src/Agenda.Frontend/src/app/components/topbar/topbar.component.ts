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
  private static readonly DEFAULT_ACCOUNT_NAME = 'Mon compte';

  public readonly isAuthenticated = input(false);
  public readonly accountName = input(TopbarComponent.DEFAULT_ACCOUNT_NAME);
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
      label: 'Planning',
      path: '/appointments',
      exact: true
    },
    {
      label: 'Nouveau rendez-vous',
      path: '/appointments/new',
      exact: true
    },
    {
      label: 'Participants',
      path: '/attendees',
      exact: true
    }
  ];

  public readonly normalizedAccountName = computed(() => {
    const normalized = this.accountName().replace(/\s+/g, ' ').trim();

    return normalized.length > 0
      ? normalized.slice(0, 80)
      : TopbarComponent.DEFAULT_ACCOUNT_NAME;
  });

  public readonly authLabel = computed(() => this.isAuthenticated() ? this.normalizedAccountName() : 'Se connecter');
  public readonly authHint = computed(() => this.isAuthenticated() ? 'Se deconnecter' : 'Connexion');

  public handleSearchInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.searchTerm.set(target.value);
    this.searchChange.emit(this.searchTerm());
  }

  public handleAuthAction(): void {
    this.authAction.emit();
  }
}
