import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, ActivatedRoute, Router } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { ConfirmationService } from '../shared/confirmation/confirmation.service';
import { DocumentViewerPage } from './document-viewer-page';

describe('DocumentViewerPage', () => {
  const documentsUrl = 'http://localhost:5076/api/documents/';

  const documentDto = {
    id: 1,
    title: 'Passport',
    fileName: 'passport.jpg',
    description: 'Travel document',
    category: 'Identity',
    expiryDate: '2030-01-01T00:00:00Z',
    uploadedAt: '2026-06-01T00:00:00Z',
  };

  function setup(routeId = '1') {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: { get: (key: string) => (key === 'id' ? routeId : null) },
            },
          },
        },
      ],
    });

    const fixture = TestBed.createComponent(DocumentViewerPage);
    const http = TestBed.inject(HttpTestingController);
    const createObjectURL = vi.fn(() => 'blob:preview');
    const revokeObjectURL = vi.fn();
    vi.stubGlobal('URL', { createObjectURL, revokeObjectURL });

    fixture.detectChanges();
    return { fixture, http, el: fixture.nativeElement as HTMLElement, createObjectURL };
  }

  function buttonByText(el: HTMLElement, text: string): HTMLButtonElement {
    return Array.from(el.querySelectorAll('button')).find(
      (b) => b.textContent?.trim().startsWith(text) || b.getAttribute('aria-label') === text,
    ) as HTMLButtonElement;
  }

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it('loads and renders the document details with an image preview', async () => {
    const { fixture, http, el, createObjectURL } = setup();

    http.expectOne(`${documentsUrl}1`).flush(documentDto);
    http.expectOne(`${documentsUrl}1/file`).flush(new Blob(['img'], { type: 'image/jpeg' }));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el.textContent).toContain('Passport');
    expect(el.textContent).toContain('passport.jpg');
    expect(el.textContent).toContain('Identity');
    expect(el.textContent).toContain('Travel document');
    expect(createObjectURL).toHaveBeenCalled();

    const img = el.querySelector('img');
    expect(img).toBeTruthy();
    expect(img?.getAttribute('src')).toBe('blob:preview');
  });

  it('renders a PDF preview in an iframe with an open-in-new-tab action', async () => {
    const { fixture, http, el } = setup();

    http.expectOne(`${documentsUrl}1`).flush({ ...documentDto, fileName: 'passport.pdf' });
    http.expectOne(`${documentsUrl}1/file`).flush(new Blob(['pdf'], { type: 'application/pdf' }));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el.querySelector('iframe')).toBeTruthy();
    expect(el.textContent).toContain('Open in new tab');
  });

  it('edits metadata and saves it through the update endpoint', async () => {
    const { fixture, http, el } = setup();

    http.expectOne(`${documentsUrl}1`).flush(documentDto);
    http.expectOne(`${documentsUrl}1/file`).flush(new Blob(['img'], { type: 'image/jpeg' }));
    await fixture.whenStable();
    fixture.detectChanges();

    buttonByText(el, 'Edit').click();
    fixture.detectChanges();

    const titleInput = el.querySelector<HTMLInputElement>('#viewer-title')!;
    titleInput.value = 'New passport';
    titleInput.dispatchEvent(new Event('input'));

    buttonByText(el, 'Save').click();

    const putRequest = http.expectOne(`${documentsUrl}1`);
    expect(putRequest.request.method).toBe('PUT');

    const body = putRequest.request.body as {
      title: string;
      description: string;
      category: string;
      expiryDate: string;
    };

    expect(body.title).toBe('New passport');
    expect(body.description).toBe('Travel document');
    expect(body.category).toBe('Identity');
    // Expiry is now sent as a full ISO timestamp (date + time), not a date-only string.
    expect(body.expiryDate).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$/);
    putRequest.flush({ ...documentDto, title: 'New passport' });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el.textContent).toContain('New passport');
  });

  it('asks for confirmation before deleting the document', async () => {
    const { fixture, http, el } = setup();
    const confirmation = TestBed.inject(ConfirmationService);
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigateByUrl').mockResolvedValue(true);

    http.expectOne(`${documentsUrl}1`).flush(documentDto);
    http.expectOne(`${documentsUrl}1/file`).flush(new Blob(['img'], { type: 'image/jpeg' }));
    await fixture.whenStable();
    fixture.detectChanges();

    // Delete opens the confirmation modal instead of deleting right away.
    buttonByText(el, 'Delete').click();
    fixture.detectChanges();

    const state = confirmation.state();
    expect(state?.title).toBe('Delete document?');
    expect(state?.description).toContain('Passport');
    expect(state?.confirmLabel).toBe('Delete');
    expect(state?.destructive).toBe(true);
    http.expectNone({ method: 'DELETE', url: `${documentsUrl}1` });

    // Confirming performs the delete and navigates back to the list.
    confirmation.accept();
    const deleteRequest = http.expectOne(`${documentsUrl}1`);
    expect(deleteRequest.request.method).toBe('DELETE');
    deleteRequest.flush(null);
    await fixture.whenStable();

    expect(navigateSpy).toHaveBeenCalledWith('/documents');
    expect(confirmation.state()).toBeNull();
  });
});
