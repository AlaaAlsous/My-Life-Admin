import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ChatMessageDto, ChatReplyDto, SendChatMessageDto } from '../models/chat.dto';

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/api/chat`;

  sendMessage(message: SendChatMessageDto): Observable<ChatReplyDto> {
    return this.http.post<ChatReplyDto>(`${this.url}/`, message);
  }

  getHistory(count?: number, documentId?: number): Observable<ChatMessageDto[]> {
    let params = new HttpParams();
    if (count !== undefined) {
      params = params.set('count', count);
    }
    if (documentId !== undefined) {
      params = params.set('documentId', documentId);
    }
    return this.http.get<ChatMessageDto[]>(`${this.url}/history`, { params });
  }
}
