import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-error-state',
  template: `
    <div class="rounded-card bg-surface-error px-4 py-3 text-error" role="alert">
      <p class="text-label">{{ message() }}</p>
      @if (retryLabel(); as label) {
        <button
          type="button"
          class="mt-3 rounded-control border border-error px-3 py-1 text-label font-medium hover:bg-white"
          (click)="retry.emit()"
        >
          {{ label }}
        </button>
      }
    </div>
  `,
})
export class ErrorState {
  readonly message = input.required<string>();
  readonly retryLabel = input<string | null>(null);

  readonly retry = output<void>();
}
