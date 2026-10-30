import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { EventDto } from '../models/event.dto';
import { ReminderDto, ReminderStatus } from '../models/reminder.dto';
import { EventService } from './event.service';
import { ReminderService } from './reminder.service';

@Component({
  selector: 'app-event',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './event.component.html',
})
export class EventComponent implements OnInit {
  private readonly eventService = inject(EventService);
  private readonly reminderService = inject(ReminderService);

  events: EventDto[] = [];
  reminders: ReminderDto[] = [];

  isLoading = false;
  errorMessage = '';
  successMessage = '';

  readonly ReminderStatus = ReminderStatus;

  reminderOptions = [
    { label: '1 week before', months: 0, days: 7 },
    { label: '1 month before', months: 1, days: 0 },
    { label: '3 months before', months: 3, days: 0 },
    { label: '6 months before', months: 6, days: 0 },
  ];

  selectedReminder: Record<number, number> = {};

  ngOnInit(): void {
    this.loadEvents();
    this.loadReminders();
  }

  loadEvents(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.eventService.getAll().subscribe({
      next: (events) => {
        this.events = events;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Could not load events.';
        this.isLoading = false;
      },
    });
  }

  loadReminders(): void {
    this.reminderService.getAll().subscribe({
      next: (reminders) => {
        this.reminders = reminders;
      },
      error: () => {
        this.errorMessage = 'Could not load reminders.';
      },
    });
  }

  getReminderForEvent(eventId: number): ReminderDto | undefined {
    return this.reminders.find(
      (reminder) => reminder.eventId === eventId,
    );
  }

  createReminder(event: EventDto): void {
    this.successMessage = '';
    this.errorMessage = '';

    const optionIndex = this.selectedReminder[event.id];

    if (optionIndex === undefined) {
      return;
    }

    const option = this.reminderOptions[optionIndex];
    const reminderDate = new Date(event.eventDate);

    if (option.months > 0) {
      reminderDate.setMonth(
        reminderDate.getMonth() - option.months,
      );
    }

    if (option.days > 0) {
      reminderDate.setDate(
        reminderDate.getDate() - option.days,
      );
    }

    this.reminderService
      .create({
        eventId: event.id,
        documentId: null,
        reminderDate: reminderDate.toISOString(),
      })
      .subscribe({
        next: (reminder) => {
          this.reminders.push(reminder);
          delete this.selectedReminder[event.id];
          this.successMessage = 'Reminder created successfully.';
        },
        error: () => {
          this.errorMessage = 'Could not create reminder.';
        },
      });
  }

  deleteReminder(reminderId: number): void {
    this.successMessage = '';
    this.errorMessage = '';

    this.reminderService.delete(reminderId).subscribe({
      next: () => {
        this.reminders = this.reminders.filter(
          (reminder) => reminder.id !== reminderId,
        );

        this.successMessage = 'Reminder deleted successfully.';
      },
      error: () => {
        this.errorMessage = 'Could not delete reminder.';
      },
    });
  }
}
