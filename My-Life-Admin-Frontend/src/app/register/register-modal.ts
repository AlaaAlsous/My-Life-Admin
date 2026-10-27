import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, output, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RegisterService } from './register.service';

@Component({
  selector: 'app-register-modal',
  imports: [ReactiveFormsModule],
  templateUrl: './register-modal.html',
})
export class RegisterModal {
  private readonly fb = inject(FormBuilder);
  private readonly registerService = inject(RegisterService);

  readonly closed = output<void>();
  readonly loggedIn = output<void>();
  readonly loginRequested = output<void>();

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  protected get name() {
    return this.form.controls.name;
  }

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
    this.registerService.register(this.form.getRawValue()).subscribe({
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
    if (err.status === 409) {
      return 'This email is already registered.';
    }
    if (err.status === 400 && typeof err.error?.error === 'string') {
      return err.error.error;
    }
    if (err.status === 0) {
      return 'Could not reach the server. Please try again later.';
    }
    return 'Something went wrong while registering. Please try again.';
  }
}
