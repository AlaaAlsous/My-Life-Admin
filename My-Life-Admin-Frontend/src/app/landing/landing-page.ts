import { Component, signal } from '@angular/core';
import { LoginModal } from '../login/login-modal';

@Component({
  selector: 'app-landing-page',
  imports: [LoginModal],
  templateUrl: './landing-page.html',
})
export class LandingPage {
  protected readonly showLogin = signal(false);
}
