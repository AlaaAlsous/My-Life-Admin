import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  CreateDocumentDto,
  DocumentAnalysisDto,
  DocumentDto,
  UpdateDocumentDto,
} from '../models/document.dto';

@Injectable({ providedIn: 'root' })
export class DocumentService {
  private readonly http = inject(HttpClient);
  private readonly url = `${environment.apiUrl}/api/documents/`;

  getAll(): Observable<DocumentDto[]> {
    return this.http.get<DocumentDto[]>(this.url);
  }

  getById(id: number): Observable<DocumentDto> {
    return this.http.get<DocumentDto>(`${this.url}${id}`);
  }

  search(query: string): Observable<DocumentDto[]> {
    const params = new HttpParams().set('q', query);
    return this.http.get<DocumentDto[]>(`${this.url}search`, { params });
  }

  getExpiring(days = 30): Observable<DocumentDto[]> {
    const params = new HttpParams().set('days', days);
    return this.http.get<DocumentDto[]>(`${this.url}expiring`, { params });
  }

  create(document: CreateDocumentDto): Observable<DocumentDto> {
    const formData = new FormData();
    formData.append('Title', document.title);
    formData.append('File', document.file);

    if (document.description != null) {
      formData.append('Description', document.description);
    }

    if (document.category != null) {
      formData.append('Category', document.category);
    }

    if (document.expiryDate != null) {
      formData.append('ExpiryDate', document.expiryDate);
    }

    return this.http.post<DocumentDto>(this.url, formData);
  }

  update(id: number, document: UpdateDocumentDto): Observable<DocumentDto> {
    return this.http.put<DocumentDto>(`${this.url}${id}`, document);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.url}${id}`);
  }

  downloadFile(id: number): Observable<Blob> {
    return this.http.get(`${this.url}${id}/file`, { responseType: 'blob' });
  }

  analyze(id: number): Observable<DocumentAnalysisDto> {
    return this.http.post<DocumentAnalysisDto>(`${this.url}${id}/analyze`, null);
  }
}
