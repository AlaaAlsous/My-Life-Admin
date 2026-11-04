import {
  Component,
  ElementRef,
  HostListener,
  inject,
  OnDestroy,
  OnInit,
  signal,
} from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { marked } from 'marked';
import { filter, Subscription } from 'rxjs';
import { ChatMessageDto } from '../models/chat.dto';
import { ChatService } from './chat.service';

@Component({
  selector: 'app-chat-widget',
  templateUrl: './chat-widget.html',
})
export class ChatWidget implements OnInit, OnDestroy {
  private readonly chatService = inject(ChatService);
  private readonly router = inject(Router);
  private readonly elementRef = inject(ElementRef);

  protected readonly open = signal(false);
  protected readonly documentId = signal<number | null>(null);
  protected readonly messages = signal<ChatMessageDto[]>([]);
  protected readonly input = signal('');
  protected readonly sending = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly loadingHistory = signal(false);

  private routerSubscription?: Subscription;

  ngOnInit(): void {
    this.routerSubscription = this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe(() => this.onRouteChanged());
    this.onRouteChanged();
  }

  ngOnDestroy(): void {
    this.routerSubscription?.unsubscribe();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.open()) {
      return;
    }

    const target = event.target as Node;

    if (!this.elementRef.nativeElement.contains(target)) {
      this.open.set(false);
    }
  }

  protected toggleOpen(): void {
    this.open.update((value) => !value);
  }

  protected sendMessage(): void {
    const text = this.input().trim();

    if (!text || this.sending()) {
      return;
    }

    this.sending.set(true);
    this.error.set(null);

    const documentId = this.documentId();
    const payload = documentId === null ? { message: text } : { message: text, documentId };

    this.chatService.sendMessage(payload).subscribe({
      next: (reply) => {
        this.messages.update((current) => [...current, reply.userMessage, reply.reply]);
        this.input.set('');
        this.sending.set(false);
      },
      error: () => {
        this.sending.set(false);
        this.error.set('The AI assistant is unavailable right now. Please try again later.');
      },
    });
  }

  protected renderMarkdown(text: string): string {
    return marked.parse(text, { async: false }) as string;
  }

  private onRouteChanged(): void {
    this.documentId.set(this.documentIdFromUrl(this.router.url));
    this.messages.set([]);
    this.input.set('');
    this.error.set(null);
    this.loadHistory();
  }

  private loadHistory(): void {
    const documentId = this.documentId();

    // Only document chats are persisted, so general chat starts empty.
    if (documentId === null) {
      this.loadingHistory.set(false);
      return;
    }

    this.loadingHistory.set(true);
    this.chatService.getHistory(50, documentId).subscribe({
      next: (messages) => {
        this.messages.set(messages);
        this.loadingHistory.set(false);
      },
      error: () => this.loadingHistory.set(false),
    });
  }

  private documentIdFromUrl(url: string): number | null {
    const match = /^\/documents\/(\d+)/.exec(url);
    return match ? Number(match[1]) : null;
  }
}
