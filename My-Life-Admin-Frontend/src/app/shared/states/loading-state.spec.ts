import { TestBed } from '@angular/core/testing';
import { LoadingState } from './loading-state';

describe('LoadingState', () => {
  function setup(inputs: Record<string, unknown> = {}) {
    const fixture = TestBed.createComponent(LoadingState);
    for (const [key, value] of Object.entries(inputs)) {
      fixture.componentRef.setInput(key, value);
    }
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('marks the content as busy with a status role', () => {
    const status = setup().querySelector('[role="status"]');
    expect(status?.getAttribute('aria-busy')).toBe('true');
  });

  it('exposes a default English label to assistive technology', () => {
    expect(setup().querySelector('.sr-only')?.textContent).toContain('Loading');
  });

  it('supports a custom label', () => {
    expect(setup({ label: 'Loading documents…' }).querySelector('.sr-only')?.textContent).toContain(
      'Loading documents…',
    );
  });

  it('renders the requested number of skeleton lines', () => {
    expect(setup({ lineCount: 5 }).querySelectorAll('[data-testid="skeleton-line"]').length).toBe(5);
  });

  it('only animates when motion is allowed', () => {
    const line = setup().querySelector('[data-testid="skeleton-line"]');
    expect(line?.className).toContain('motion-safe:animate-pulse');
    expect(line?.className).not.toMatch(/(^|\s)animate-pulse/);
  });
});
