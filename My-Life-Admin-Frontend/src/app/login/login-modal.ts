import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LoginService } from './login.service';

@Component({
  selector: 'app-login-modal',
  imports: [ReactiveFormsModule],
  templateUrl: './login-modal.html',
})
export class LoginModal {
  private readonly fb = inject(FormBuilder);
  private readonly loginService = inject(LoginService);

  readonly closed = output<void>();
  readonly loggedIn = output<void>();

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(6)]],
  });

  protected get email() {
    return this.form.controls.email;
  }

  protected get password() {
    return this.form.controls.password;
  }

  protected submit(): void {
    this.errorMessage.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.loginService.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.submitting.set(false);
        this.loggedIn.emit();
        this.closed.emit();
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        this.errorMessage.set(this.toMessage(err));
      },
    });
  }

  protected close(): void {
    this.closed.emit();
  }

  private toMessage(err: HttpErrorResponse): string {
    if (err.status === 400 || err.status === 401) {
      return 'Incorrect email or password.';
    }
    if (err.status === 0) {
      return 'Could not reach the server. Please try again later.';
    }
    return 'Something went wrong while logging in. Please try again.';
  }
}
