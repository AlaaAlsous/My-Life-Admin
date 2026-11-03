import { TestBed } from '@angular/core/testing';
import { ConfirmationService } from './confirmation.service';

describe('ConfirmationService', () => {
  let service: ConfirmationService;

  beforeEach(() => {
    service = TestBed.inject(ConfirmationService);
  });

  it('is closed by default', () => {
    expect(service.state()).toBeNull();
  });

  it('opens with default labels', () => {
    void service.confirm({ title: 'Delete?' });
    expect(service.state()).toEqual(
      expect.objectContaining({ title: 'Delete?', confirmLabel: 'Confirm', cancelLabel: 'Cancel' }),
    );
  });

  it('uses custom text and labels', () => {
    void service.confirm({
      title: 'Discard?',
      description: 'Changes are lost.',
      confirmLabel: 'Discard',
      cancelLabel: 'Keep editing',
    });
    expect(service.state()).toEqual(
      expect.objectContaining({
        description: 'Changes are lost.',
        confirmLabel: 'Discard',
        cancelLabel: 'Keep editing',
      }),
    );
  });

  it('runs onConfirm, closes and resolves true on accept', async () => {
    const onConfirm = vi.fn();
    const result = service.confirm({ title: 'Delete?', onConfirm });

    service.accept();

    expect(await result).toBe(true);
    expect(onConfirm).toHaveBeenCalledTimes(1);
    expect(service.state()).toBeNull();
  });

  it('does not run onConfirm, closes and resolves false on cancel', async () => {
    const onConfirm = vi.fn();
    const result = service.confirm({ title: 'Delete?', onConfirm });

    service.cancel();

    expect(await result).toBe(false);
    expect(onConfirm).not.toHaveBeenCalled();
    expect(service.state()).toBeNull();
  });

  it('treats a replaced request as cancelled', async () => {
    const onConfirm = vi.fn();
    const first = service.confirm({ title: 'First', onConfirm });
    void service.confirm({ title: 'Second' });

    expect(await first).toBe(false);
    expect(onConfirm).not.toHaveBeenCalled();
    expect(service.state()?.title).toBe('Second');
  });
});
