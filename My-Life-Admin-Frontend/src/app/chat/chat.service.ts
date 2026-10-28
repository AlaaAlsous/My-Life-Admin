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

  getHistory(count?: number): Observable<ChatMessageDto[]> {
    const params = count === undefined ? new HttpParams() : new HttpParams().set('count', count);
    return this.http.get<ChatMessageDto[]>(`${this.url}/history`, { params });
  }
}
