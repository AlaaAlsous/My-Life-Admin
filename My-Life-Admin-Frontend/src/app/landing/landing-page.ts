import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthTokenService } from '../auth/auth-token.service';
import { LoginModal } from '../login/login-modal';
import { RegisterModal } from '../register/register-modal';

@Component({
  selector: 'app-landing-page',
  imports: [LoginModal, RegisterModal, RouterLink],
  templateUrl: './landing-page.html',
})
export class LandingPage {
  protected readonly features = [
    {
      title: 'Personal Documents',
      description: 'Keep important documents, such as passports and insurance policies, organized.',
      route: '/documents',
      icon: 'M19.5 14.25v-2.625a3.375 3.375 0 0 0-3.375-3.375h-1.5A1.125 1.125 0 0 1 13.5 7.125v-1.5a3.375 3.375 0 0 0-3.375-3.375H8.25m0 12.75h7.5m-7.5 3H12M10.5 2.25H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 0 0-9-9Z',
    },
    {
      title: 'Receipts and Purchases',
      description: 'Keep track of receipts and purchase information.',
      route: '/documents',
      icon: 'M15.75 10.5V6a3.75 3.75 0 1 0-7.5 0v4.5m11.356-1.993 1.263 12c.07.665-.45 1.243-1.119 1.243H4.25a1.125 1.125 0 0 1-1.12-1.243l1.264-12A1.125 1.125 0 0 1 5.513 7.5h12.974c.576 0 1.059.435 1.119 1.007ZM8.625 10.5a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0Zm7.5 0a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0Z',
    },
    {
      title: 'Personal Information',
      description: 'Organize important personal details in one place.',
      route: '/dashboard',
      icon: 'M15 9h3.75M15 12h3.75M15 15h3.75M4.5 19.5h15a2.25 2.25 0 0 0 2.25-2.25V6.75A2.25 2.25 0 0 0 19.5 4.5h-15a2.25 2.25 0 0 0-2.25 2.25v10.5A2.25 2.25 0 0 0 4.5 19.5Zm6-10.125a1.875 1.875 0 1 1-3.75 0 1.875 1.875 0 0 1 3.75 0Zm1.294 6.336a6.721 6.721 0 0 1-3.17.789 6.721 6.721 0 0 1-3.168-.789 3.376 3.376 0 0 1 6.338 0Z',
    },
    {
      title: 'Reminders and Important Dates',
      description: 'Keep track of deadlines, renewals and other important dates.',
      route: '/events',
      icon: 'M14.857 17.082a23.848 23.848 0 0 0 5.454-1.31A8.967 8.967 0 0 1 18 9.75V9A6 6 0 0 0 6 9v.75a8.967 8.967 0 0 1-2.312 6.022c1.733.64 3.56 1.085 5.455 1.31m5.714 0a24.255 24.255 0 0 1-5.714 0m5.714 0a3 3 0 1 1-5.714 0',
    },
    {
      title: 'Everyday Administration',
      description: 'Make personal administration easier to manage.',
      route: '/dashboard',
      icon: 'M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z',
    },
  ];

  private readonly router = inject(Router);
  private readonly authTokenService = inject(AuthTokenService);

  protected readonly isLoggedIn = this.authTokenService.isLoggedIn;
  protected readonly showLogin = signal(false);
  protected readonly showRegister = signal(false);

  protected switchToRegister(): void {
    this.showLogin.set(false);
    this.showRegister.set(true);
  }

  protected switchToLogin(): void {
    this.showRegister.set(false);
    this.showLogin.set(true);
  }

  protected onLoggedIn(): void {
    this.router.navigateByUrl('/dashboard');
  }
}
