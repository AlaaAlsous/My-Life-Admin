import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { DocumentService } from './document.service';

describe('DocumentService', () => {
  const documentsUrl = 'http://localhost:5076/api/documents/';
  let service: DocumentService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(DocumentService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('gets, searches, and filters documents by expiry', () => {
    service.getAll().subscribe();
    http.expectOne(documentsUrl).flush([]);

    service.getById(12).subscribe();
    http.expectOne(`${documentsUrl}12`).flush({});

    service.search('passport').subscribe();
    const searchRequest = http.expectOne(`${documentsUrl}search?q=passport`);
    searchRequest.flush([]);

    service.getExpiring().subscribe();
    const expiringRequest = http.expectOne(`${documentsUrl}expiring?days=30`);
    expiringRequest.flush([]);
  });

  it('creates documents as multipart form data and updates document metadata', () => {
    const file = new File(['contents'], 'passport.pdf', { type: 'application/pdf' });
    const document = {
      title: 'Passport',
      description: 'Travel document',
      category: 'Identity',
      expiryDate: '2030-01-01',
      file,
    };

    service.create(document).subscribe();
    const createRequest = http.expectOne(documentsUrl);
    const formData = createRequest.request.body as FormData;
    expect(createRequest.request.method).toBe('POST');
    expect(formData.get('Title')).toBe('Passport');
    expect(formData.get('Description')).toBe('Travel document');
    expect(formData.get('Category')).toBe('Identity');
    expect(formData.get('ExpiryDate')).toBe('2030-01-01');
    expect(formData.get('File')).toBe(file);
    createRequest.flush({});

    const update = {
      title: 'Updated passport',
      description: null,
      category: 'Identity',
      expiryDate: null,
    };
    service.update(12, update).subscribe();
    const updateRequest = http.expectOne(`${documentsUrl}12`);
    expect(updateRequest.request.method).toBe('PUT');
    expect(updateRequest.request.body).toEqual(update);
    updateRequest.flush({});
  });

  it('deletes documents and downloads their files', () => {
    service.delete(12).subscribe();
    const deleteRequest = http.expectOne(`${documentsUrl}12`);
    expect(deleteRequest.request.method).toBe('DELETE');
    deleteRequest.flush(null);

    service.downloadFile(12).subscribe();
    const downloadRequest = http.expectOne(`${documentsUrl}12/file`);
    expect(downloadRequest.request.responseType).toBe('blob');
    downloadRequest.flush(new Blob(['contents']));
  });
});
