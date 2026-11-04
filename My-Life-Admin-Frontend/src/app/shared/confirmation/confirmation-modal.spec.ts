import { TestBed } from '@angular/core/testing';
import { ConfirmationModal } from './confirmation-modal';
import { ConfirmationService } from './confirmation.service';

describe('ConfirmationModal', () => {
  function setup() {
    const fixture = TestBed.createComponent(ConfirmationModal);
    document.body.appendChild(fixture.nativeElement);
    fixture.detectChanges();
    const service = TestBed.inject(ConfirmationService);
    const el = fixture.nativeElement as HTMLElement;
    const open = async (options: Parameters<ConfirmationService['confirm']>[0]) => {
      void service.confirm(options);
      fixture.detectChanges();
      await fixture.whenStable();
      fixture.detectChanges();
    };
    const buttons = () => Array.from(el.querySelectorAll('button'));
    return { fixture, service, el, open, buttons };
  }

  afterEach(() => {
    document.body.innerHTML = '';
  });

  it('renders nothing while closed', () => {
    expect(setup().el.querySelector('[role="alertdialog"]')).toBeNull();
  });

  it('renders custom title, description and labels with dialog semantics', async () => {
    const { el, open, buttons } = setup();
    await open({
      title: 'Delete event?',
      description: 'This cannot be undone.',
      confirmLabel: 'Delete',
      cancelLabel: 'Keep',
    });

    const dialog = el.querySelector('[role="alertdialog"]')!;
    expect(dialog.getAttribute('aria-modal')).toBe('true');
    expect(dialog.querySelector('#confirmation-title')?.textContent).toContain('Delete event?');
    expect(dialog.textContent).toContain('This cannot be undone.');
    expect(buttons().map((b) => b.textContent?.trim())).toEqual(['Keep', 'Delete']);
  });

  it('runs onConfirm and closes when confirm is clicked', async () => {
    const { el, open, buttons, fixture } = setup();
    const onConfirm = vi.fn();
    await open({ title: 'Sure?', onConfirm });

    buttons()[1].click();
    fixture.detectChanges();

    expect(onConfirm).toHaveBeenCalledTimes(1);
    expect(el.querySelector('[role="alertdialog"]')).toBeNull();
  });

  it('does not run onConfirm when cancel is clicked', async () => {
    const { el, open, buttons, fixture } = setup();
    const onConfirm = vi.fn();
    await open({ title: 'Sure?', onConfirm });

    buttons()[0].click();
    fixture.detectChanges();

    expect(onConfirm).not.toHaveBeenCalled();
    expect(el.querySelector('[role="alertdialog"]')).toBeNull();
  });

  it('cancels on Escape and on backdrop click', async () => {
    const { el, open, fixture } = setup();
    const onConfirm = vi.fn();

    await open({ title: 'Sure?', onConfirm });
    el.querySelector('[role="presentation"]')!.dispatchEvent(
      new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }),
    );
    fixture.detectChanges();
    expect(el.querySelector('[role="alertdialog"]')).toBeNull();

    await open({ title: 'Sure?', onConfirm });
    (el.querySelector('[role="presentation"]') as HTMLElement).click();
    fixture.detectChanges();
    expect(el.querySelector('[role="alertdialog"]')).toBeNull();
    expect(onConfirm).not.toHaveBeenCalled();
  });

  it('does not close when clicking inside the dialog', async () => {
    const { el, open, fixture } = setup();
    await open({ title: 'Sure?' });

    (el.querySelector('[role="alertdialog"]') as HTMLElement).click();
    fixture.detectChanges();

    expect(el.querySelector('[role="alertdialog"]')).not.toBeNull();
  });

  it('focuses cancel on open, traps Tab and restores focus on close', async () => {
    const { el, open, buttons, fixture } = setup();
    const trigger = document.createElement('button');
    document.body.appendChild(trigger);
    trigger.focus();

    await open({ title: 'Sure?' });
    const [cancel, confirm] = buttons();
    expect(document.activeElement).toBe(cancel);

    confirm.focus();
    const tab = new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true });
    confirm.dispatchEvent(tab);
    expect(tab.defaultPrevented).toBe(true);
    expect(document.activeElement).toBe(cancel);

    const shiftTab = new KeyboardEvent('keydown', {
      key: 'Tab',
      shiftKey: true,
      bubbles: true,
      cancelable: true,
    });
    cancel.dispatchEvent(shiftTab);
    expect(document.activeElement).toBe(confirm);

    cancel.click();
    fixture.detectChanges();
    await fixture.whenStable();
    expect(el.querySelector('[role="alertdialog"]')).toBeNull();
    expect(document.activeElement).toBe(trigger);
  });
});
