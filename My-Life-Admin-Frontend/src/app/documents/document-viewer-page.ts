import { DatePipe } from '@angular/common';
import { Component, computed, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DocumentDto } from '../models/document.dto';
import { ConfirmationService } from '../shared/confirmation/confirmation.service';
import { ErrorState } from '../shared/states/error-state';
import { LoadingState } from '../shared/states/loading-state';
import { DocumentService } from './document.service';

@Component({
  selector: 'app-document-viewer-page',
  imports: [DatePipe, RouterLink, ErrorState, LoadingState],
  templateUrl: './document-viewer-page.html',
})
export class DocumentViewerPage implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly documentService = inject(DocumentService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly confirmation = inject(ConfirmationService);

  protected readonly loading = signal(true);
  protected readonly failed = signal(false);
  protected readonly document = signal<DocumentDto | null>(null);

  protected readonly previewUrl = signal<string | null>(null);
  protected readonly previewLoading = signal(false);

  protected readonly editing = signal(false);
  protected readonly saving = signal(false);
  protected readonly saveError = signal<string | null>(null);
  protected readonly title = signal('');
  protected readonly category = signal('');
  protected readonly description = signal('');
  protected readonly expiryLocal = signal('');

  protected readonly isPdf = computed(
    () => this.document()?.fileName.toLowerCase().endsWith('.pdf') ?? false,
  );

  protected readonly previewSrc = computed<SafeResourceUrl | null>(() => {
    const url = this.previewUrl();
    return url ? this.sanitizer.bypassSecurityTrustResourceUrl(url) : null;
  });

  ngOnInit(): void {
    this.load();
  }

  ngOnDestroy(): void {
    this.revokePreview();
  }

  protected load(): void {
    const id = this.documentId();

    if (id === null) {
      this.loading.set(false);
      this.failed.set(true);
      return;
    }

    this.loading.set(true);
    this.failed.set(false);

    this.documentService.getById(id).subscribe({
      next: (document) => {
        this.document.set(document);
        this.loading.set(false);
        this.loadPreview(id);
      },
      error: () => {
        this.loading.set(false);
        this.failed.set(true);
      },
    });
  }

  protected startEditing(): void {
    const document = this.document();

    if (!document) {
      return;
    }

    this.title.set(document.title);
    this.category.set(document.category ?? '');
    this.description.set(document.description ?? '');
    this.expiryLocal.set(document.expiryDate ? this.toLocalInput(document.expiryDate) : '');
    this.saveError.set(null);
    this.editing.set(true);
  }

  protected cancelEditing(): void {
    this.editing.set(false);
    this.saveError.set(null);
  }

  protected save(): void {
    const id = this.documentId();

    if (id === null) {
      return;
    }

    const title = this.title().trim();

    if (!title) {
      this.saveError.set('A title is required.');
      return;
    }

    this.saving.set(true);
    this.saveError.set(null);

    this.documentService
      .update(id, {
        title,
        category: this.category().trim() || null,
        description: this.description().trim() || null,
        expiryDate: this.expiryIso() || null,
      })
      .subscribe({
        next: (updated) => {
          this.saving.set(false);
          this.document.set(updated);
          this.editing.set(false);
        },
        error: () => {
          this.saving.set(false);
          this.saveError.set('Could not save the details. Please try again.');
        },
      });
  }

  protected deleteDocument(): void {
    const id = this.documentId();

    if (id === null) {
      return;
    }

    const document = this.document();

    void this.confirmation.confirm({
      title: 'Delete document?',
      description: document
        ? `"${document.title}" will be permanently deleted. This cannot be undone.`
        : 'This document will be permanently deleted. This cannot be undone.',
      confirmLabel: 'Delete',
      cancelLabel: 'Cancel',
      destructive: true,
      onConfirm: () => {
        this.documentService.delete(id).subscribe({
          next: () => this.router.navigateByUrl('/documents'),
        });
      },
    });
  }

  protected openFile(): void {
    const url = this.previewUrl();

    if (url) {
      window.open(url, '_blank', 'noopener');
    }
  }

  protected download(): void {
    const id = this.documentId();
    const document = this.document();

    if (id === null || !document) {
      return;
    }

    this.documentService.downloadFile(id).subscribe({
      next: (blob) => this.saveBlob(blob, document.fileName),
    });
  }

  private loadPreview(id: number): void {
    this.previewLoading.set(true);

    this.documentService.downloadFile(id).subscribe({
      next: (blob) => {
        this.revokePreview();
        this.previewUrl.set(URL.createObjectURL(blob));
        this.previewLoading.set(false);
      },
      error: () => this.previewLoading.set(false),
    });
  }

  private revokePreview(): void {
    const url = this.previewUrl();

    if (url) {
      URL.revokeObjectURL(url);
      this.previewUrl.set(null);
    }
  }

  private saveBlob(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(url);
  }

  private documentId(): number | null {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    return Number.isInteger(id) && id > 0 ? id : null;
  }

  private expiryIso(): string {
    const value = this.expiryLocal().trim();

    if (!value) {
      return '';
    }

    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
      return '';
    }

    return date.toISOString();
  }

  private toLocalInput(value: string): string {
    const date = new Date(value);

    if (Number.isNaN(date.getTime())) {
      return '';
    }

    const pad = (n: number) => n.toString().padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }
}
