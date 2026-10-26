import { Component, computed, inject, input, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { LoginModal } from '../login/login-modal';

export interface NavLink {
  path: string;
  label: string;
}

@Component({
  selector: 'app-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, LoginModal],
  templateUrl: './app-layout.html',
})
export class AppLayout {
  private readonly router = inject(Router);

  readonly status = input('');

  protected readonly showLogin = signal(false);
  protected readonly menuOpen = signal(false);

  // Only configured, navigable routes get a link, so there are never dead links.
  protected readonly navLinks = computed<NavLink[]>(() =>
    this.router.config
      .filter((r) => r.path && !r.redirectTo && !r.path.includes('*') && !r.path.includes(':'))
      .map((r) => ({
        path: r.path as string,
        label: typeof r.title === 'string' ? r.title : this.toLabel(r.path as string),
      })),
  );

  protected readonly currentYear = new Date().getFullYear();

  protected toggleMenu(): void {
    this.menuOpen.update((open) => !open);
  }

  protected closeMenu(): void {
    this.menuOpen.set(false);
  }

  private toLabel(path: string): string {
    const text = path.replace(/[-/]+/g, ' ').trim();
    return text.charAt(0).toUpperCase() + text.slice(1);
  }
}
