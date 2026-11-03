import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { SettingsService } from './settings.service';

describe('SettingsService', () => {
  const settingsUrl = 'http://localhost:5076/api/settings';
  let service: SettingsService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(SettingsService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('gets the current settings', () => {
    const settings = { id: 3, userId: 7 };

    service.getSettings().subscribe((result) => {
      expect(result).toEqual(settings);
    });

    const request = http.expectOne(settingsUrl);
    expect(request.request.method).toBe('GET');
    request.flush(settings);
  });

  it('updates settings', () => {
    const settings = { id: 3, userId: 7 };

    service.updateSettings(settings).subscribe();

    const request = http.expectOne(settingsUrl);
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(settings);
    request.flush(null);
  });
});
