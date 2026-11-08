import { DatePipe } from '@angular/common';
import { afterNextRender, Component, computed, ElementRef, inject, Injector, OnDestroy, OnInit, signal, viewChild,} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { EventDto } from '../models/event.dto';
import { ReminderDto, ReminderStatus } from '../models/reminder.dto';
import { ErrorState } from '../shared/states/error-state';
import { LoadingState } from '../shared/states/loading-state';
import { buildMonthGrid, CalendarDay } from './calendar.utils';
import { ConfirmationService } from '../shared/confirmation/confirmation.service';
import { EventCalendar } from './event-calendar';
import { EventService } from './event.service';
import { ReminderService } from './reminder.service';
import { EventTimeline } from './event-timeline';

@Component({
  selector: 'app-event',
  standalone: true,
  imports: [
    DatePipe,
    FormsModule,
    RouterLink,
    ErrorState,
    LoadingState,
    EventCalendar,
    EventTimeline,
  ],
  templateUrl: './event.component.html',
})
export class EventComponent implements OnInit, OnDestroy {
  private readonly eventService = inject(EventService);
  private readonly reminderService = inject(ReminderService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly injector = inject(Injector);
  private readonly eventDetails = viewChild<ElementRef<HTMLElement>>('eventDetails');

  private eventsRequest?: Subscription;
  private allEventsRequest?: Subscription;

  protected readonly month = signal(this.startOfMonth(new Date()));
  protected readonly events = signal<EventDto[]>([]);
  protected readonly allEvents = signal<EventDto[]>([]);
  protected readonly reminders = signal<ReminderDto[]>([]);

  protected readonly isLoading = signal(false);
  protected readonly isLoadingAllEvents = signal(false);
  protected readonly errorMessage = signal('');
  protected readonly successMessage = signal('');

  protected readonly selectedDate = signal<Date | null>(null);
  protected readonly selectedEvents = signal<EventDto[] | null>(null);
  protected readonly showTimeline = signal(false);

  protected readonly editingEventId = signal<number | null>(null);
  protected readonly savingEvent = signal(false);

  protected editTitle = '';
  protected editDescription = '';
  protected editCategory = '';
  protected editDate = '';

  protected readonly sortedEvents = computed(() =>
    [...this.allEvents()].sort(
      (a, b) => new Date(a.eventDate).getTime() - new Date(b.eventDate).getTime(),
    ),
  );

  protected readonly ReminderStatus = ReminderStatus;

  protected readonly reminderOptions = [
    { label: '1 week before', months: 0, days: 7 },
    { label: '1 month before', months: 1, days: 0 },
    { label: '3 months before', months: 3, days: 0 },
    { label: '6 months before', months: 6, days: 0 },
    { label: 'Custom date and time', months: 0, days: 0 },
  ];

  protected readonly customReminderOption = 4;
  protected selectedReminder: Record<number, number | undefined> = {};
  protected selectedEditReminder: Record<number, number | undefined> = {};
  protected customReminderDate: Record<number, string> = {};
  protected customEditReminderDate: Record<number, string> = {};

  ngOnInit(): void {
    this.loadEvents();
    this.loadAllEvents();
    this.loadReminders();
  }

  ngOnDestroy(): void {
    this.eventsRequest?.unsubscribe();
    this.allEventsRequest?.unsubscribe();
  }

  protected toggleView(): void {
    this.showTimeline.update((value) => !value);
  }

  protected loadEvents(): void {
    this.isLoading.set(true);
    this.errorMessage.set('');
    this.eventsRequest?.unsubscribe();

    const grid = buildMonthGrid(this.month(), []);
    const from = grid[0].date;
    const lastDay = grid[grid.length - 1].date;

    const to = new Date(
      lastDay.getFullYear(),
      lastDay.getMonth(),
      lastDay.getDate(),
      23,
      59,
      59,
      999,
    );

    this.eventsRequest = this.eventService.getAll(from.toISOString(), to.toISOString()).subscribe({
      next: (events) => {
        this.events.set(events);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load events.');
        this.isLoading.set(false);
      },
    });
  }

  protected loadAllEvents(): void {
    this.isLoadingAllEvents.set(true);
    this.allEventsRequest?.unsubscribe();

    this.allEventsRequest = this.eventService.getAll().subscribe({
      next: (events) => {
        this.allEvents.set(events);
        this.isLoadingAllEvents.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load all events.');
        this.isLoadingAllEvents.set(false);
      },
    });
  }

  protected loadReminders(): void {
    this.reminderService.getAll().subscribe({
      next: (reminders) => this.reminders.set(reminders),
      error: () => this.errorMessage.set('Could not load reminders.'),
    });
  }

  protected onMonthChange(month: Date): void {
    this.month.set(this.startOfMonth(month));
    this.clearSelection();
    this.loadEvents();
  }

  protected onEventSelected(event: EventDto): void {
    this.cancelEditing();
    this.selectedDate.set(new Date(event.eventDate));
    this.selectedEvents.set([event]);

    afterNextRender(
      () => {
        this.eventDetails()?.nativeElement.scrollIntoView({
          behavior: 'smooth',
          block: 'start',
        });
      },
      { injector: this.injector },
    );
  }

  protected onDaySelected(day: CalendarDay): void {
    this.cancelEditing();
    this.selectedDate.set(day.date);
    this.selectedEvents.set(day.events);
  }

  protected clearSelection(): void {
    this.cancelEditing();
    this.selectedDate.set(null);
    this.selectedEvents.set(null);
  }

  protected startEditing(event: EventDto): void {
    this.editingEventId.set(event.id);

    this.editTitle = event.title;
    this.editDescription = event.description ?? '';
    this.editCategory = event.category;

    const date = new Date(event.eventDate);
    const localDate = new Date(date.getTime() - date.getTimezoneOffset() * 60000);

    this.editDate = localDate.toISOString().slice(0, 16);
  }

  protected cancelEditing(): void {
    this.editingEventId.set(null);
  }

  protected saveEvent(): void {
    const id = this.editingEventId();

    if (id === null || this.savingEvent() || !this.editTitle.trim() || !this.editDate) {
      return;
    }

    const originalEvent = this.allEvents().find((event) => event.id === id);

    if (!originalEvent) {
      this.errorMessage.set('Could not find event.');
      return;
    }

    this.savingEvent.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.eventService
      .update(id, {
        documentId: originalEvent.documentId,
        title: this.editTitle.trim(),
        description: this.editDescription.trim() || null,
        category: this.editCategory.trim(),
        eventDate: new Date(this.editDate).toISOString(),
      })
      .subscribe({
        next: (updatedEvent) => {
          this.allEvents.update((events) =>
            events.map((event) => (event.id === id ? updatedEvent : event)),
          );

          this.loadEvents();

          this.selectedEvents.update(
            (events) => events?.map((event) => (event.id === id ? updatedEvent : event)) ?? null,
          );

          this.savingEvent.set(false);
          this.editingEventId.set(null);
          this.successMessage.set('Event updated successfully.');
        },
        error: () => {
          this.savingEvent.set(false);
          this.errorMessage.set('Could not update event.');
        },
      });
  }

  protected deleteEvent(event: EventDto): void {
    void this.confirmation.confirm({
      title: 'Delete event?',
      description: `Are you sure you want to delete "${event.title}"? This cannot be undone.`,
      confirmLabel: 'Delete',
      cancelLabel: 'Cancel',
      destructive: true,
      onConfirm: () => this.performDeleteEvent(event.id),
    });
  }

  private performDeleteEvent(eventId: number): void {
    this.errorMessage.set('');
    this.successMessage.set('');

    this.eventService.delete(eventId).subscribe({
      next: () => {
        this.events.update((events) => events.filter((event) => event.id !== eventId));

        this.allEvents.update((events) => events.filter((event) => event.id !== eventId));

        this.selectedEvents.update(
          (events) => events?.filter((event) => event.id !== eventId) ?? null,
        );

        this.reminders.update((reminders) =>
          reminders.filter((reminder) => reminder.eventId !== eventId),
        );

        this.editingEventId.set(null);
        this.successMessage.set('Event deleted successfully.');
      },
      error: () => {
        this.errorMessage.set('Could not delete event.');
      },
    });
  }

  protected getReminderForEvent(eventId: number): ReminderDto | undefined {
    return this.reminders().find((reminder) => reminder.eventId === eventId);
  }

  protected createReminder(event: EventDto): void {
    this.successMessage.set('');
    this.errorMessage.set('');

    const optionIndex = this.selectedReminder[event.id];

    if (optionIndex === undefined) {
      return;
    }

    const reminderDate = this.getSelectedReminderDate(
      event,
      optionIndex,
      this.customReminderDate[event.id],
    );

    if (!reminderDate) {
      this.errorMessage.set('Please select a valid reminder date and time.');
      return;
    }

    this.reminderService
      .create({
        eventId: event.id,
        documentId: null,
        reminderDate,
      })
      .subscribe({
        next: (reminder) => {
          this.reminders.update((reminders) => [...reminders, reminder]);

          delete this.selectedReminder[event.id];
          delete this.customReminderDate[event.id];

          this.successMessage.set('Reminder created successfully.');
        },
        error: () => {
          this.errorMessage.set('Could not create reminder.');
        },
      });
  }

  protected deleteReminder(reminderId: number): void {
    void this.confirmation.confirm({
      title: 'Delete reminder?',
      description: 'This reminder will be permanently deleted. This cannot be undone.',
      confirmLabel: 'Delete',
      cancelLabel: 'Cancel',
      destructive: true,
      onConfirm: () => this.performDeleteReminder(reminderId),
    });
  }

  private performDeleteReminder(reminderId: number): void {
    this.successMessage.set('');
    this.errorMessage.set('');

    this.reminderService.delete(reminderId).subscribe({
      next: () => {
        this.reminders.update((reminders) =>
          reminders.filter((reminder) => reminder.id !== reminderId),
        );

        this.successMessage.set('Reminder deleted successfully.');
      },
      error: () => {
        this.errorMessage.set('Could not delete reminder.');
      },
    });
  }

  protected updateReminder(event: EventDto, reminder: ReminderDto): void {
    this.successMessage.set('');
    this.errorMessage.set('');

    const optionIndex = this.selectedEditReminder[event.id];

    if (optionIndex === undefined) {
      return;
    }

    const reminderDate = this.getSelectedReminderDate(
      event,
      optionIndex,
      this.customEditReminderDate[event.id],
    );

    if (!reminderDate) {
      this.errorMessage.set('Please select a valid reminder date and time.');
      return;
    }

    this.reminderService
      .update(reminder.id, {
        eventId: event.id,
        documentId: reminder.documentId,
        reminderDate,
      })
      .subscribe({
        next: (updatedReminder) => {
          this.reminders.update((reminders) =>
            reminders.map((existing) =>
              existing.id === updatedReminder.id ? updatedReminder : existing,
            ),
          );

          delete this.selectedEditReminder[event.id];
          delete this.customEditReminderDate[event.id];

          this.successMessage.set('Reminder updated successfully.');
        },
        error: () => {
          this.errorMessage.set('Could not update reminder.');
        },
      });
  }

  private getSelectedReminderDate(
    event: EventDto,
    optionIndex: number,
    customDate: string | undefined,
  ): string | null {
    if (optionIndex === this.customReminderOption) {
      if (!customDate) {
        return null;
      }

      const date = new Date(customDate);

      if (Number.isNaN(date.getTime())) {
        return null;
      }

      return date.toISOString();
    }

    return this.reminderDateFor(event, optionIndex);
  }

  private reminderDateFor(event: EventDto, optionIndex: number): string {
    const option = this.reminderOptions[optionIndex];
    const reminderDate = new Date(event.eventDate);

    if (option.months > 0) {
      reminderDate.setMonth(reminderDate.getMonth() - option.months);
    }

    if (option.days > 0) {
      reminderDate.setDate(reminderDate.getDate() - option.days);
    }

    return reminderDate.toISOString();
  }

  private startOfMonth(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), 1);
  }
}
