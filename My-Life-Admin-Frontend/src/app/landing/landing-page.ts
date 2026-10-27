import { Component, signal } from '@angular/core';
import { LoginModal } from '../login/login-modal';

@Component({
  selector: 'app-landing-page',
  imports: [LoginModal],
  templateUrl: './landing-page.html',
})
export class LandingPage {
  protected readonly features = [
    {
      title: 'Personal Documents',
      description: 'Keep important documents, such as passports and insurance policies, organized.',
    },
    {
      title: 'Receipts and Purchases',
      description: 'Keep track of receipts and purchase information.',
    },
    {
      title: 'Personal Information',
      description: 'Organize important personal details in one place.',
    },
    {
      title: 'Reminders and Important Dates',
      description: 'Keep track of deadlines, renewals and other important dates.',
    },
    {
      title: 'Everyday Administration',
      description: 'Make personal administration easier to manage.',
    },
  ];

  protected readonly showLogin = signal(false);
}
