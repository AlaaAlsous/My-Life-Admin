import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { DocumentsPage } from './documents-page';
import { AuthTokenService } from '../auth/auth-token.service';
import { ConfirmationService } from '../shared/confirmation/confirmation.service';

describe('DocumentsPage', () => {
  const documentsUrl = 'http://localhost:5076/api/documents/';

  it('loads documents when the user logs in after the page is created', () => {
    const { fixture, http, tokenService } = setup(false);
    http.expectNone(documentsUrl);

    tokenService.setToken('test-token');
    fixture.detectChanges();

    http.expectOne(documentsUrl).flush([]);
  });

  function setup(loggedIn = true) {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    const tokenService = TestBed.inject(AuthTokenService);
    if (loggedIn) {
      tokenService.setToken('test-token');
    }
    const fixture = TestBed.createComponent(DocumentsPage);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    return { fixture, http, tokenService, el: fixture.nativeElement as HTMLElement };
  }

  function uploadButton(el: HTMLElement): HTMLButtonElement {
    return Array.from(el.querySelectorAll('button')).find(
      (b) => b.textContent?.trim() === 'Upload',
    ) as HTMLButtonElement;
  }

  afterEach(() => {
    localStorage.clear();
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it('loads and renders the document list', async () => {
    const { fixture, http, el } = setup();
    http.expectOne(documentsUrl).flush([
      {
        id: 1,
        title: 'Passport',
        fileName: 'passport.pdf',
        description: null,
        category: 'Identity',
        expiryDate: null,
        uploadedAt: '2026-06-01T00:00:00Z',
      },
    ]);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el.textContent).toContain('Passport');
    expect(el.textContent).toContain('passport.pdf');
    expect(el.textContent).toContain('Identity');
  });

  it('requires a file before uploading', () => {
    const { fixture, el, http } = setup();
    http.expectOne(documentsUrl).flush([]);

    uploadButton(el).click();
    fixture.detectChanges();

    expect(el.textContent).toContain('A file is required.');
    http.expectNone(documentsUrl);
  });

  it('uploads a document and reloads the list', async () => {
    const { fixture, http, el } = setup();
    http.expectOne(documentsUrl).flush([]);

    const titleInput = el.querySelector<HTMLInputElement>('#doc-title')!;
    titleInput.value = 'Passport';
    titleInput.dispatchEvent(new Event('input'));

    const file = new File(['contents'], 'passport.pdf', { type: 'application/pdf' });
    const fileInput = el.querySelector<HTMLInputElement>('#doc-file')!;
    Object.defineProperty(fileInput, 'files', { configurable: true, value: [file] });
    fileInput.dispatchEvent(new Event('change'));

    uploadButton(el).click();

    const uploadRequest = http.expectOne(documentsUrl);
    expect(uploadRequest.request.method).toBe('POST');
    expect(uploadRequest.request.body).toBeInstanceOf(FormData);
    const formData = uploadRequest.request.body as FormData;
    expect(formData.get('Title')).toBe('Passport');
    expect(formData.get('File')).toBe(file);
    uploadRequest.flush({
      id: 1,
      title: 'Passport',
      fileName: 'passport.pdf',
      description: null,
      category: null,
      expiryDate: null,
      uploadedAt: '2026-06-01T00:00:00Z',
    });

    http.expectOne(documentsUrl).flush([
      {
        id: 1,
        title: 'Passport',
        fileName: 'passport.pdf',
        description: null,
        category: null,
        expiryDate: null,
        uploadedAt: '2026-06-01T00:00:00Z',
      },
    ]);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el.textContent).toContain('Passport');
  });

  it('triggers a blob download', async () => {
    const { fixture, http, el } = setup();
    http.expectOne(documentsUrl).flush([
      {
        id: 1,
        title: 'Passport',
        fileName: 'passport.pdf',
        description: null,
        category: null,
        expiryDate: null,
        uploadedAt: '2026-06-01T00:00:00Z',
      },
    ]);
    await fixture.whenStable();
    fixture.detectChanges();

    const blob = new Blob(['contents'], { type: 'application/pdf' });
    const createObjectURL = vi.fn(() => 'blob:download');
    const revokeObjectURL = vi.fn();
    vi.stubGlobal('URL', { createObjectURL, revokeObjectURL });
    const anchorClick = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => {});

    el.querySelector<HTMLButtonElement>('ul button')!.click();
    const downloadRequest = http.expectOne(`${documentsUrl}1/file`);
    expect(downloadRequest.request.responseType).toBe('blob');
    downloadRequest.flush(blob);

    expect(createObjectURL).toHaveBeenCalledWith(blob);
    expect(anchorClick).toHaveBeenCalled();
    expect(revokeObjectURL).toHaveBeenCalledWith('blob:download');
  });

  const doc = {
    id: 1,
    title: 'Passport',
    fileName: 'passport.pdf',
    description: null,
    category: null,
    expiryDate: null,
    uploadedAt: '2026-06-01T00:00:00Z',
  };

  it('does not delete when the confirmation is cancelled', async () => {
    const { fixture, http, el } = setup();
    http.expectOne(documentsUrl).flush([doc]);
    await fixture.whenStable();
    fixture.detectChanges();

    el.querySelector<HTMLButtonElement>('ul li button:last-child')!.click();
    TestBed.inject(ConfirmationService).cancel();

    http.expectNone(`${documentsUrl}1`);
    expect(TestBed.inject(ConfirmationService).state()).toBeNull();
  });

  it('reloads the list after confirming deletion of a document', async () => {
    const { fixture, http, el } = setup();
    http.expectOne(documentsUrl).flush([doc]);
    await fixture.whenStable();
    fixture.detectChanges();

    el.querySelector<HTMLButtonElement>('ul li button:last-child')!.click();
    const confirmation = TestBed.inject(ConfirmationService);
    expect(confirmation.state()?.title).toBe('Delete document?');
    http.expectNone(`${documentsUrl}1`);

    confirmation.accept();
    const deleteRequest = http.expectOne(`${documentsUrl}1`);
    expect(deleteRequest.request.method).toBe('DELETE');
    deleteRequest.flush(null);

    http.expectOne(documentsUrl).flush([]);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el.textContent).toContain('No documents yet');
  });

  it('deletes the uploaded document when review is cancelled', async () => {
    const { fixture, http, el } = setup();
    http.expectOne(documentsUrl).flush([]);

    const file = new File(['contents'], 'passport.pdf', { type: 'application/pdf' });
    const fileInput = el.querySelector<HTMLInputElement>('#doc-file')!;
    Object.defineProperty(fileInput, 'files', { configurable: true, value: [file] });
    fileInput.dispatchEvent(new Event('change'));

    uploadButton(el).click();

    const uploadRequest = http.expectOne(documentsUrl);
    expect(uploadRequest.request.method).toBe('POST');
    uploadRequest.flush({
      id: 1,
      title: 'Passport',
      fileName: 'passport.pdf',
      description: null,
      category: null,
      expiryDate: null,
      uploadedAt: '2026-06-01T00:00:00Z',
    });

    // The upload triggers a list refresh and then an analysis call.
    http.expectOne(documentsUrl).flush([]);
    http.expectOne(`${documentsUrl}1/analyze`).flush({
      title: null,
      category: null,
      expiryDate: null,
      description: null,
    });
    await fixture.whenStable();
    fixture.detectChanges();

    // The form is now in review mode with a Cancel button.
    const cancelButton = Array.from(el.querySelectorAll<HTMLButtonElement>('button')).find(
      (b) => b.textContent?.trim() === 'Cancel',
    )!;
    expect(cancelButton).toBeTruthy();
    cancelButton.click();

    const deleteRequest = http.expectOne(`${documentsUrl}1`);
    expect(deleteRequest.request.method).toBe('DELETE');
    deleteRequest.flush(null);

    await fixture.whenStable();
    fixture.detectChanges();
  });
});
