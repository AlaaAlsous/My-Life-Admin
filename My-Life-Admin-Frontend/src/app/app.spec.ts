import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { ConfirmationService } from './shared/confirmation/confirmation.service';

describe('App', () => {
  beforeEach(async () => {
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

  it('should render the layout and show the backend status', async () => {
    const fixture = TestBed.createComponent(App);
    TestBed.inject(HttpTestingController)
      .expectOne('http://localhost:5076/api/health')
      .flush({ status: 'Healthy' });
    await fixture.whenStable();
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('app-layout')).toBeTruthy();
    expect(compiled.querySelector('footer')?.textContent).toContain('Healthy');
  });
});
