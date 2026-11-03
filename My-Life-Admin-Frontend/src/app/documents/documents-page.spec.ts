import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { DocumentsPage } from './documents-page';

describe('DocumentsPage', () => {
  const documentsUrl = 'http://localhost:5076/api/documents/';

  function setup() {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const fixture = TestBed.createComponent(DocumentsPage);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    return { fixture, http, el: fixture.nativeElement as HTMLElement };
  }

  afterEach(() => {
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

    el.querySelector<HTMLButtonElement>('button')!.click();
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

    el.querySelector<HTMLButtonElement>('button')!.click();

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

  it('reloads the list after deleting a document', async () => {
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

    el.querySelector<HTMLButtonElement>('ul li button:last-child')!.click();
    const deleteRequest = http.expectOne(`${documentsUrl}1`);
    expect(deleteRequest.request.method).toBe('DELETE');
    deleteRequest.flush(null);

    http.expectOne(documentsUrl).flush([]);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el.textContent).toContain('No documents yet');
  });
});
