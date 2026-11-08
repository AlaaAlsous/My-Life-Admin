import { DatePipe } from '@angular/common';
import { Component, computed, effect, inject, signal, untracked } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthTokenService } from '../auth/auth-token.service';
import { DocumentAnalysisDto, DocumentDto } from '../models/document.dto';
import { EmptyState } from '../shared/states/empty-state';
import { ErrorState } from '../shared/states/error-state';
import { LoadingState } from '../shared/states/loading-state';
import { DocumentService } from './document.service';
import { ConfirmationService } from '../shared/confirmation/confirmation.service';

@Component({
  selector: 'app-documents-page',
  imports: [DatePipe, RouterLink, EmptyState, ErrorState, LoadingState],
  templateUrl: './documents-page.html',
})
export class DocumentsPage {
  private readonly documentService = inject(DocumentService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly authTokenService = inject(AuthTokenService);

  protected readonly loading = signal(true);
  protected readonly failed = signal(false);
  protected readonly documents = signal<DocumentDto[]>([]);

  protected readonly searchQuery = signal('');

  protected readonly filteredDocuments = computed(() => {
    const query = this.searchQuery().trim().toLowerCase();
    const category = this.selectedCategory();

    return this.documents().filter((document) => {
      const matchesSearch = document.title.toLowerCase().includes(query);
      const matchesCategory = !category || document.category === category;

      return matchesSearch && matchesCategory;
    });
  });

  protected readonly selectedCategory = signal('');

  protected readonly categories = computed(() =>
    [
      ...new Set(
        this.documents()
          .map((document) => document.category)
          .filter((category): category is string => !!category),
      ),
    ].sort(),
  );

  protected isExpiringSoon(document: DocumentDto): boolean {
    if (!document.expiryDate) return false;

    const today = new Date();
    const expiryDate = new Date(document.expiryDate);

    today.setHours(0, 0, 0, 0);

    const daysUntilExpiry = (expiryDate.getTime() - today.getTime()) / (1000 * 60 * 60 * 24);

    return daysUntilExpiry >= 0 && daysUntilExpiry <= 30;
  }

  protected readonly title = signal('');
  protected readonly category = signal('');
  protected readonly description = signal('');
  protected readonly expiryDate = signal('');
  protected readonly file = signal<File | null>(null);

  protected readonly uploading = signal(false);
  protected readonly analyzing = signal(false);
  protected readonly saving = signal(false);
  protected readonly uploadError = signal<string | null>(null);
  protected readonly analysisNote = signal<string | null>(null);
  protected readonly reviewingId = signal<number | null>(null);

  constructor() {
    effect(() => {
      if (this.authTokenService.isLoggedIn()) {
        untracked(() => this.load());
      } else {
        untracked(() => {
          this.documents.set([]);
          this.loading.set(false);
        });
      }
    });
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

  protected submit(): void {
    if (this.reviewingId() !== null) {
      this.saveConfirmed();
    } else {
      this.upload();
    }
  }

  protected upload(): void {
    const selectedFile = this.file();

    if (!selectedFile) {
      this.uploadError.set('A file is required.');
      return;
    }

    this.uploading.set(true);
    this.uploadError.set(null);
    this.analysisNote.set(null);

    const title = this.title().trim() || this.fileNameWithoutExtension(selectedFile.name);

    this.documentService
      .create({
        title,
        category: this.category().trim() || null,
        description: this.description().trim() || null,
        expiryDate: this.expiryDate() || null,
        file: selectedFile,
      })
      .subscribe({
        next: (created) => {
          this.uploading.set(false);
          this.reviewingId.set(created.id);
          this.load();
          this.analyze(created.id);
        },
        error: () => {
          this.uploading.set(false);
          this.uploadError.set('Upload failed. Please try again.');
        },
      });
  }

  protected saveConfirmed(): void {
    const id = this.reviewingId();

    if (id === null) {
      return;
    }

    const title = this.title().trim();

    if (!title) {
      this.uploadError.set('A title is required.');
      return;
    }

    this.saving.set(true);
    this.uploadError.set(null);

    this.documentService
      .update(id, {
        title,
        category: this.category().trim() || null,
        description: this.description().trim() || null,
        expiryDate: this.expiryDate() || null,
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.resetForm();
          this.load();
        },
        error: () => {
          this.saving.set(false);
          this.uploadError.set('Could not save the details. Please try again.');
        },
      });
  }

  protected cancelReview(): void {
    this.resetForm();
    this.load();
  }

  protected download(document: DocumentDto): void {
    this.documentService.downloadFile(document.id).subscribe({
      next: (blob) => this.saveBlob(blob, document.fileName),
      error: () => this.uploadError.set('Download failed. Please try again.'),
    });
  }

  protected deleteDocument(id: number): void {
    const document = this.documents().find((d) => d.id === id);
    void this.confirmation.confirm({
      title: 'Delete document?',
      description: document
        ? `"${document.title}" will be permanently deleted. This cannot be undone.`
        : 'This document will be permanently deleted. This cannot be undone.',
      confirmLabel: 'Delete',
      cancelLabel: 'Cancel',
      destructive: true,
      onConfirm: () => {
        this.documentService.delete(id).subscribe(() => this.load());
      },
    });
  }

  private analyze(id: number): void {
    this.analyzing.set(true);
    this.analysisNote.set(null);

    this.documentService.analyze(id).subscribe({
      next: (result) => {
        this.analyzing.set(false);
        this.applyAnalysis(result);
      },
      error: () => {
        this.analyzing.set(false);
        this.analysisNote.set(
          'We could not read the file automatically. Please enter the details manually.',
        );
      },
    });
  }

  private applyAnalysis(result: DocumentAnalysisDto): void {
    const hasValue = result.title || result.category || result.description || result.expiryDate;

    if (hasValue) {
      if (result.title) {
        this.title.set(result.title);
      }
      if (result.category) {
        this.category.set(result.category);
      }
      if (result.description) {
        this.description.set(result.description);
      }
      if (result.expiryDate) {
        this.expiryDate.set(this.toDateInput(result.expiryDate));
      }
      this.analysisNote.set(
        'We read the file and filled in the details. Review them and save when ready.',
      );
    } else {
      this.analysisNote.set(
        'We could not read the file automatically. Please enter the details manually.',
      );
    }
  }

  private resetForm(): void {
    this.title.set('');
    this.category.set('');
    this.description.set('');
    this.expiryDate.set('');
    this.file.set(null);
    this.reviewingId.set(null);
    this.analysisNote.set(null);
    this.uploadError.set(null);
  }

  private fileNameWithoutExtension(name: string): string {
    const lastDot = name.lastIndexOf('.');
    return lastDot > 0 ? name.slice(0, lastDot) : name;
  }

  private toDateInput(value: string): string {
    return value ? value.slice(0, 10) : '';
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
