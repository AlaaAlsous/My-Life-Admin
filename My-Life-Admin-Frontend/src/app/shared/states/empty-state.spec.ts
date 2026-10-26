import { TestBed } from '@angular/core/testing';
import { EmptyState } from './empty-state';

describe('EmptyState', () => {
  function setup(inputs: Record<string, unknown> = {}) {
    const fixture = TestBed.createComponent(EmptyState);
    fixture.componentRef.setInput('title', 'No documents yet');
    fixture.componentRef.setInput('message', 'Documents you add will appear here.');
    for (const [key, value] of Object.entries(inputs)) {
      fixture.componentRef.setInput(key, value);
    }
    fixture.detectChanges();
    return { fixture, el: fixture.nativeElement as HTMLElement };
  }

  it('renders the title and message', () => {
    const { el } = setup();
    expect(el.querySelector('h2')?.textContent).toContain('No documents yet');
    expect(el.querySelector('p')?.textContent).toContain('Documents you add will appear here.');
  });

  it('does not render an action by default', () => {
    expect(setup().el.querySelector('button')).toBeNull();
  });

  it('renders an action button and emits when clicked', () => {
    const { fixture, el } = setup({ actionLabel: 'Add document' });
    let emitted = 0;
    fixture.componentInstance.action.subscribe(() => emitted++);

    const button = el.querySelector('button');
    expect(button?.textContent).toContain('Add document');
    button?.click();
    expect(emitted).toBe(1);
  });
});
