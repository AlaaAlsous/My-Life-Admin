import { TestBed } from '@angular/core/testing';
import { ErrorState } from './error-state';

describe('ErrorState', () => {
  function setup(inputs: Record<string, unknown> = {}) {
    const fixture = TestBed.createComponent(ErrorState);
    fixture.componentRef.setInput('message', 'Could not load your documents.');
    for (const [key, value] of Object.entries(inputs)) {
      fixture.componentRef.setInput(key, value);
    }
    fixture.detectChanges();
    return { fixture, el: fixture.nativeElement as HTMLElement };
  }

  it('announces the message with role="alert"', () => {
    const alert = setup().el.querySelector('[role="alert"]');
    expect(alert?.textContent).toContain('Could not load your documents.');
  });

  it('does not render a retry button by default', () => {
    expect(setup().el.querySelector('button')).toBeNull();
  });

  it('renders a retry button and emits when clicked', () => {
    const { fixture, el } = setup({ retryLabel: 'Try again' });
    let emitted = 0;
    fixture.componentInstance.retry.subscribe(() => emitted++);

    const button = el.querySelector('button');
    expect(button?.textContent).toContain('Try again');
    button?.click();
    expect(emitted).toBe(1);
  });
});
