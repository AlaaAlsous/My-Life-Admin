import { DatePipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
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

@Component({
  selector: 'app-event',
  standalone: true,
  imports: [DatePipe, FormsModule, RouterLink, ErrorState, LoadingState, EventCalendar],
  templateUrl: './event.component.html',
})
export class EventComponent implements OnInit, OnDestroy {
  private readonly eventService = inject(EventService);
  private readonly reminderService = inject(ReminderService);
  private readonly confirmation = inject(ConfirmationService);
  private eventsRequest?: Subscription;

  protected readonly month = signal(this.startOfMonth(new Date()));
  protected readonly events = signal<EventDto[]>([]);
  protected readonly reminders = signal<ReminderDto[]>([]);
  protected readonly isLoading = signal(false);
  protected readonly errorMessage = signal('');
  protected readonly successMessage = signal('');
  protected readonly selectedDate = signal<Date | null>(null);
  protected readonly selectedEvents = signal<EventDto[] | null>(null);
  protected readonly showTimeline = signal(false);

  protected toggleView(): void {
    this.showTimeline.update(value => !value);
  }

  protected readonly ReminderStatus = ReminderStatus;

  protected readonly reminderOptions = [
    { label: '1 week before', months: 0, days: 7 },
    { label: '1 month before', months: 1, days: 0 },
    { label: '3 months before', months: 3, days: 0 },
    { label: '6 months before', months: 6, days: 0 },
  ];

  protected selectedReminder: Record<number, number | undefined> = {};
  protected selectedEditReminder: Record<number, number | undefined> = {};

  ngOnInit(): void {
    this.loadEvents();
    this.loadReminders();
  }

  ngOnDestroy(): void {
    this.eventsRequest?.unsubscribe();
  }

  protected loadEvents(): void {
    this.isLoading.set(true);
    this.errorMessage.set('');
    this.eventsRequest?.unsubscribe();

    // The visible grid includes days from the neighbouring months.
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
    this.selectedDate.set(null);
    this.selectedEvents.set([event]);
  }

  protected onDaySelected(day: CalendarDay): void {
    this.selectedDate.set(day.date);
    this.selectedEvents.set(day.events);
  }

  protected clearSelection(): void {
    this.selectedDate.set(null);
    this.selectedEvents.set(null);
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

    this.reminderService
      .create({
        eventId: event.id,
        documentId: null,
        reminderDate: this.reminderDateFor(event, optionIndex),
      })
      .subscribe({
        next: (reminder) => {
          this.reminders.update((reminders) => [...reminders, reminder]);
          delete this.selectedReminder[event.id];
          this.successMessage.set('Reminder created successfully.');
        },
        error: () => this.errorMessage.set('Could not create reminder.'),
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
      error: () => this.errorMessage.set('Could not delete reminder.'),
    });
  }

  protected updateReminder(event: EventDto, reminder: ReminderDto): void {
    this.successMessage.set('');
    this.errorMessage.set('');

    const optionIndex = this.selectedEditReminder[event.id];

    if (optionIndex === undefined) {
      return;
    }

    this.reminderService
      .update(reminder.id, {
        eventId: event.id,
        documentId: reminder.documentId,
        reminderDate: this.reminderDateFor(event, optionIndex),
      })
      .subscribe({
        next: (updatedReminder) => {
          this.reminders.update((reminders) =>
            reminders.map((existing) =>
              existing.id === updatedReminder.id ? updatedReminder : existing,
            ),
          );
          delete this.selectedEditReminder[event.id];
          this.successMessage.set('Reminder updated successfully.');
        },
        error: () => this.errorMessage.set('Could not update reminder.'),
      });
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
