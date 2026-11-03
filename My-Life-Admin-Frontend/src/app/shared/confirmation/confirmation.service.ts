import { Injectable, signal } from '@angular/core';

export interface ConfirmationOptions {
  title: string;
  description?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  destructive?: boolean;
  onConfirm?: () => void | Promise<void>;
}

export interface ConfirmationState extends ConfirmationOptions {
  confirmLabel: string;
  cancelLabel: string;
}

@Injectable({ providedIn: 'root' })
export class ConfirmationService {
  private readonly _state = signal<ConfirmationState | null>(null);
  private resolver: ((confirmed: boolean) => void) | null = null;

  readonly state = this._state.asReadonly();

  /**
   * Opens the global confirmation modal. Resolves true only if the user
   * explicitly confirms; resolves false on cancel, Escape or backdrop click.
   * `onConfirm` is invoked only on explicit confirmation.
   */
  confirm(options: ConfirmationOptions): Promise<boolean> {
    // A new request replaces any open one, which counts as cancelled.
    this.settle(false);

    this._state.set({
      ...options,
      confirmLabel: options.confirmLabel ?? 'Confirm',
      cancelLabel: options.cancelLabel ?? 'Cancel',
    });

    return new Promise<boolean>((resolve) => {
      this.resolver = resolve;
    });
  }

  accept(): void {
    const onConfirm = this._state()?.onConfirm;
    this.settle(true);
    void onConfirm?.();
  }

  cancel(): void {
    this.settle(false);
  }

  private settle(result: boolean): void {
    const resolve = this.resolver;
    this.resolver = null;
    this._state.set(null);
    resolve?.(result);
  }
}
