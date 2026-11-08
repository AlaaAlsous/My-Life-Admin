import { DatePipe } from '@angular/common';
import { Component, computed, input, output } from '@angular/core';
import { EventDto } from '../models/event.dto';
import { addMonths, buildMonthGrid, CalendarDay, WEEKDAY_LABELS } from './calendar.utils';

const MAX_VISIBLE_EVENTS = 2;

@Component({
  selector: 'app-event-calendar',
  imports: [DatePipe],
  templateUrl: './event-calendar.html',
})
export class EventCalendar {
  readonly events = input<EventDto[]>([]);
  readonly month = input<Date>(new Date());

  readonly selectedDate = input<Date | null>(null);

  readonly monthChange = output<Date>();
  readonly eventSelected = output<EventDto>();
  readonly daySelected = output<CalendarDay>();

  protected readonly weekdays = WEEKDAY_LABELS;
  protected readonly maxVisible = MAX_VISIBLE_EVENTS;

  protected readonly days = computed(() => buildMonthGrid(this.month(), this.events()));
  protected readonly monthStart = computed(
    () => new Date(this.month().getFullYear(), this.month().getMonth(), 1),
  );
  protected readonly hasEventsInMonth = computed(() =>
    this.days().some((day) => day.inCurrentMonth && day.events.length > 0),
  );

  protected previous(): void {
    this.monthChange.emit(addMonths(this.month(), -1));
  }

  protected next(): void {
    this.monthChange.emit(addMonths(this.month(), 1));
  }

  protected today(): void {
    this.monthChange.emit(new Date());
  }

  protected selectEvent(event: EventDto, mouseEvent: Event): void {
    mouseEvent.stopPropagation();
    this.eventSelected.emit(event);
  }

  protected selectDay(day: CalendarDay): void {
    this.daySelected.emit(day);
  }

  protected isSelected(day: CalendarDay): boolean {
    const selected = this.selectedDate();

    if (!selected) {
      return false;
    }

    return (
      day.date.getFullYear() === selected.getFullYear() &&
      day.date.getMonth() === selected.getMonth() &&
      day.date.getDate() === selected.getDate()
    );
  }
}
