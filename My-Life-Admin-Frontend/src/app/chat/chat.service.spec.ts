import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../environments/environment';
import { ChatMessageDto, ChatReplyDto } from '../models/chat.dto';
import { ChatService } from './chat.service';

describe('ChatService', () => {
  const chatUrl = `${environment.apiUrl}/api/chat`;
  const userMessage: ChatMessageDto = {
    id: 1,
    role: 'user',
    content: 'Hello',
    createdAt: '2026-10-28T12:00:00Z',
  };
  const reply: ChatMessageDto = {
    id: 2,
    role: 'assistant',
    content: 'Hello!',
    createdAt: '2026-10-28T12:00:01Z',
  };
  let service: ChatService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(ChatService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('sends a message and returns the user message and reply', () => {
    const response: ChatReplyDto = { userMessage, reply };
    let result: ChatReplyDto | undefined;

    service.sendMessage({ message: 'Hello' }).subscribe((value) => (result = value));
    const request = http.expectOne(`${chatUrl}/`);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ message: 'Hello' });
    request.flush(response);

    expect(result).toEqual(response);
  });

  it('gets a document history without a count so the backend default applies', () => {
    let result: ChatMessageDto[] | undefined;

    service.getHistory(undefined, 7).subscribe((value) => (result = value));
    const request = http.expectOne(`${chatUrl}/history?documentId=7`);
    expect(request.request.method).toBe('GET');
    request.flush([userMessage, reply]);

    expect(result).toEqual([userMessage, reply]);
  });

  it('passes the requested history count and document id', () => {
    service.getHistory(10, 7).subscribe();
    const request = http.expectOne(`${chatUrl}/history?count=10&documentId=7`);
    expect(request.request.method).toBe('GET');
    request.flush([]);
  });

  it('propagates backend errors to the caller', () => {
    let error: HttpErrorResponse | undefined;

    service.sendMessage({ message: 'Hello' }).subscribe({
      error: (value: HttpErrorResponse) => (error = value),
    });
    http
      .expectOne(`${chatUrl}/`)
      .flush(
        { error: 'The AI assistant is unavailable right now. Please try again later.' },
        { status: 502, statusText: 'Bad Gateway' },
      );

    expect(error?.status).toBe(502);
    expect(error?.error).toEqual({
      error: 'The AI assistant is unavailable right now. Please try again later.',
    });
  });
});
