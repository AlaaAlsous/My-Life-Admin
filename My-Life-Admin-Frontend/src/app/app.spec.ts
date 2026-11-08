import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { AuthTokenService } from './auth/auth-token.service';
import { ConfirmationService } from './shared/confirmation/confirmation.service';

describe('App', () => {
  beforeEach(async () => {
    localStorage.removeItem('my-life-admin.auth-token');
    localStorage.removeItem('my-life-admin.dark-mode');
    document.documentElement.classList.remove('dark');
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('shows the global confirmation modal when a confirmation is requested', async () => {
    const fixture = TestBed.createComponent(App);
    TestBed.inject(HttpTestingController).expectOne('http://localhost:5076/api/health');
    fixture.detectChanges();

    void TestBed.inject(ConfirmationService).confirm({ title: 'Delete event?' });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('app-confirmation-modal')).toBeTruthy();
    expect(compiled.querySelector('[role="alertdialog"]')?.textContent).toContain('Delete event?');
  });

  it('should render the layout', async () => {
    const fixture = TestBed.createComponent(App);
    TestBed.inject(HttpTestingController)
      .expectOne('http://localhost:5076/api/health')
      .flush({ status: 'Healthy' });
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('app-layout')).toBeTruthy();
  });

  it('loads the signed-in user theme across the app', async () => {
    TestBed.inject(AuthTokenService).setToken('jwt');
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);

    http.expectOne('http://localhost:5076/api/health').flush({ status: 'Healthy' });
    http
      .expectOne('http://localhost:5076/api/settings')
      .flush({ id: 1, userId: 1, darkMode: true, emailNotifications: true });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(document.documentElement.classList.contains('dark')).toBe(true);
    expect(localStorage.getItem('my-life-admin.dark-mode')).toBe('true');
    TestBed.inject(AuthTokenService).clearToken();
  });
});
