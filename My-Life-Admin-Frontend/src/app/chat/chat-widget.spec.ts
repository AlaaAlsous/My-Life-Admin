import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { ChatWidget } from './chat-widget';

@Component({ template: '' })
class BlankPage {}

describe('ChatWidget', () => {
  const chatUrl = 'http://localhost:5076/api/chat/';
  const historyUrl = 'http://localhost:5076/api/chat/history';

  async function setup(path: string) {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([
          { path: 'dashboard', component: BlankPage },
          { path: 'documents/:id', component: BlankPage },
        ]),
      ],
    });

    const fixture = TestBed.createComponent(ChatWidget);
    const http = TestBed.inject(HttpTestingController);
    const router = TestBed.inject(Router);

    await router.navigateByUrl(path);
    fixture.detectChanges();

    return { fixture, http, router, el: fixture.nativeElement as HTMLElement };
  }

  function buttonByLabel(el: HTMLElement, label: string): HTMLButtonElement {
    return Array.from(el.querySelectorAll('button')).find(
      (button) => button.getAttribute('aria-label') === label,
    ) as HTMLButtonElement;
  }

  function buttonByText(el: HTMLElement, text: string): HTMLButtonElement {
    return Array.from(el.querySelectorAll('button')).find(
      (button) => button.textContent?.trim() === text,
    ) as HTMLButtonElement;
  }

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('opens and closes the chat panel from the blob', async () => {
    const { fixture, el } = await setup('/dashboard');

    const closedBlob = buttonByLabel(el, 'Open AI chat');
    expect(closedBlob).toBeTruthy();
    expect(el.textContent).not.toContain('AI assistant');

    closedBlob.click();
    fixture.detectChanges();
    expect(el.textContent).toContain('AI assistant');

    const openBlob = buttonByLabel(el, 'Close AI chat');
    openBlob.click();
    fixture.detectChanges();
    expect(el.textContent).not.toContain('AI assistant');
  });

  it('sends a document-scoped message when viewing a document', async () => {
    const { fixture, http, el } = await setup('/documents/7');

    http.expectOne(`${historyUrl}?count=50&documentId=7`).flush([]);
    await fixture.whenStable();

    buttonByLabel(el, 'Open AI chat').click();
    fixture.detectChanges();

    const input = el.querySelector<HTMLInputElement>('#chat-input')!;
    input.value = 'When does this expire?';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    buttonByText(el, 'Send').click();

    const request = http.expectOne(chatUrl);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ message: 'When does this expire?', documentId: 7 });
    request.flush({
      userMessage: { id: 1, role: 'user', content: 'When does this expire?', createdAt: '' },
      reply: { id: 2, role: 'assistant', content: 'In 2030.', createdAt: '' },
    });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(el.textContent).toContain('In 2030.');
  });

  it('sends a general message when not viewing a document', async () => {
    const { fixture, http, el } = await setup('/dashboard');

    buttonByLabel(el, 'Open AI chat').click();
    fixture.detectChanges();

    const input = el.querySelector<HTMLInputElement>('#chat-input')!;
    input.value = 'What documents do I have?';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    buttonByText(el, 'Send').click();

    const request = http.expectOne(chatUrl);
    expect(request.request.body).toEqual({ message: 'What documents do I have?' });
    request.flush({
      userMessage: { id: 1, role: 'user', content: 'What documents do I have?', createdAt: '' },
      reply: { id: 2, role: 'assistant', content: 'I can help with that.', createdAt: '' },
    });
    await fixture.whenStable();
  });

  it('loads and shows the saved document history', async () => {
    const { fixture, http, el } = await setup('/documents/7');

    http.expectOne(`${historyUrl}?count=50&documentId=7`).flush([
      { id: 1, role: 'user', content: 'When does this expire?', createdAt: '' },
      { id: 2, role: 'assistant', content: 'In 2030.', createdAt: '' },
    ]);
    await fixture.whenStable();
    fixture.detectChanges();

    buttonByLabel(el, 'Open AI chat').click();
    fixture.detectChanges();

    expect(el.textContent).toContain('When does this expire?');
    expect(el.textContent).toContain('In 2030.');
  });
});
