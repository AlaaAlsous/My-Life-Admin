import { Component, ElementRef, effect, inject, viewChild } from '@angular/core';
import { ConfirmationService } from './confirmation.service';

const FOCUSABLE = 'button:not([disabled])';

@Component({
  selector: 'app-confirmation-modal',
  template: `
    @if (service.state(); as state) {
      <div
        class="fixed inset-0 z-[60] flex items-end justify-center bg-plum/60 p-4 backdrop-blur-sm sm:items-center"
        role="presentation"
        (click)="service.cancel()"
        (keydown.escape)="service.cancel()"
        (keydown)="trapFocus($event)"
      >
        <div
          #dialog
          class="w-full max-w-md rounded-card bg-white p-6 shadow-card ring-1 ring-border-soft sm:p-8"
          role="alertdialog"
          aria-modal="true"
          aria-labelledby="confirmation-title"
          [attr.aria-describedby]="state.description ? 'confirmation-description' : null"
          (click)="$event.stopPropagation()"
        >
          <h2 id="confirmation-title" class="text-2xl text-plum">{{ state.title }}</h2>
          @if (state.description) {
            <p id="confirmation-description" class="mt-3 text-text-muted">
              {{ state.description }}
            </p>
          }
          <div class="mt-6 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
            <button
              #cancelButton
              type="button"
              class="rounded-full border border-border bg-white px-5 py-2.5 font-semibold text-text hover:bg-surface-muted"
              (click)="service.cancel()"
            >
              {{ state.cancelLabel }}
            </button>
            <button
              type="button"
              class="rounded-full px-5 py-2.5 font-semibold text-white shadow-soft transition"
              [class.bg-primary]="!state.destructive"
              [class.hover:bg-primary-hover]="!state.destructive"
              [class.bg-error]="state.destructive"
              [class.hover:opacity-90]="state.destructive"
              (click)="service.accept()"
            >
              {{ state.confirmLabel }}
            </button>
          </div>
        </div>
      </div>
    }
  `,
})
export class ConfirmationModal {
  protected readonly service = inject(ConfirmationService);

  private readonly dialog = viewChild<ElementRef<HTMLElement>>('dialog');
  private readonly cancelButton = viewChild<ElementRef<HTMLButtonElement>>('cancelButton');
  private previouslyFocused: HTMLElement | null = null;

  constructor() {
    effect(() => {
      const button = this.cancelButton();
      if (button) {
        this.previouslyFocused = document.activeElement as HTMLElement | null;
        // Default focus on the safe action so Enter never confirms by accident.
        button.nativeElement.focus();
      } else if (this.previouslyFocused) {
        this.previouslyFocused.focus();
        this.previouslyFocused = null;
      }
    });
  }

  protected trapFocus(event: KeyboardEvent): void {
    if (event.key !== 'Tab') return;
    const dialog = this.dialog()?.nativeElement;
    if (!dialog) return;
    const items = Array.from(dialog.querySelectorAll<HTMLElement>(FOCUSABLE));
    if (items.length === 0) return;

    const first = items[0];
    const last = items[items.length - 1];
    const active = document.activeElement;
    const shift = event.shiftKey;

    if (shift && (active === first || !dialog.contains(active))) {
      event.preventDefault();
      last.focus();
    } else if (!shift && (active === last || !dialog.contains(active))) {
      event.preventDefault();
      first.focus();
    }
  }
}
