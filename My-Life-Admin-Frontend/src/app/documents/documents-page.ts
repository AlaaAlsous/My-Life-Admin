import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { DocumentDto } from '../models/document.dto';
import { EmptyState } from '../shared/states/empty-state';
import { ErrorState } from '../shared/states/error-state';
import { LoadingState } from '../shared/states/loading-state';
import { DocumentService } from './document.service';

@Component({
  selector: 'app-documents-page',
  imports: [DatePipe, EmptyState, ErrorState, LoadingState],
  templateUrl: './documents-page.html',
})
export class DocumentsPage {
  private readonly documentService = inject(DocumentService);

  protected readonly loading = signal(true);
  protected readonly failed = signal(false);
  protected readonly documents = signal<DocumentDto[]>([]);

  protected readonly title = signal('');
  protected readonly category = signal('');
  protected readonly description = signal('');
  protected readonly expiryDate = signal('');
  protected readonly file = signal<File | null>(null);

  protected readonly uploading = signal(false);
  protected readonly uploadError = signal<string | null>(null);

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.failed.set(false);
    this.documentService.getAll().subscribe({
      next: (documents) => {
        this.documents.set(documents);
        this.loading.set(false);
      },
      error: () => {
        this.failed.set(true);
        this.loading.set(false);
      },
    });
  }

  protected onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.file.set(input.files?.[0] ?? null);
  }

  protected upload(): void {
    const selectedFile = this.file();

    if (!selectedFile || !this.title().trim()) {
      this.uploadError.set('A title and a file are required.');
      return;
    }

    this.uploading.set(true);
    this.uploadError.set(null);

    this.documentService
      .create({
        title: this.title().trim(),
        category: this.category().trim() || null,
        description: this.description().trim() || null,
        expiryDate: this.expiryDate() || null,
        file: selectedFile,
      })
      .subscribe({
        next: () => {
          this.uploading.set(false);
          this.title.set('');
          this.category.set('');
          this.description.set('');
          this.expiryDate.set('');
          this.file.set(null);
          this.load();
        },
        error: () => {
          this.uploading.set(false);
          this.uploadError.set('Upload failed. Please try again.');
        },
      });
  }

  protected download(document: DocumentDto): void {
    this.documentService.downloadFile(document.id).subscribe({
      next: (blob) => this.saveBlob(blob, document.fileName),
      error: () => this.uploadError.set('Download failed. Please try again.'),
    });
  }

  protected deleteDocument(id: number): void {
    this.documentService.delete(id).subscribe(() => this.load());
  }

  private saveBlob(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(url);
  }
}
