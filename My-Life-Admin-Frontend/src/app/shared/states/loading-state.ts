import { Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-loading-state',
  template: `
    <div role="status" aria-busy="true" class="space-y-3">
      <span class="sr-only">{{ label() }}</span>
      @for (line of lines(); track line) {
        <div
          data-testid="skeleton-line"
          aria-hidden="true"
          class="h-4 rounded-control bg-border/30 motion-safe:animate-pulse"
        ></div>
      }
    </div>
  `,
})
export class LoadingState {
  readonly label = input('Loading…');
  readonly lineCount = input(3);

  protected readonly lines = computed(() =>
    Array.from({ length: Math.max(1, this.lineCount()) }, (_, i) => i),
  );
}
