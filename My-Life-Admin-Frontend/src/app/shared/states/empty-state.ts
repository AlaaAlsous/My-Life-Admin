import { Component, input, output } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  template: `
    <div class="rounded-card border border-border bg-surface-muted p-6 text-center">
      <h2 class="text-title font-semibold">{{ title() }}</h2>
      <p class="mt-2 text-text-muted">{{ message() }}</p>
      @if (actionLabel(); as label) {
        <button
          type="button"
          class="mt-4 rounded-control bg-primary px-4 py-2 font-medium text-white hover:bg-primary-hover"
          (click)="action.emit()"
        >
          {{ label }}
        </button>
      }
    </div>
  `,
})
export class EmptyState {
  readonly title = input.required<string>();
  readonly message = input.required<string>();
  readonly actionLabel = input<string | null>(null);

  readonly action = output<void>();
}
