import { Component, computed, input, output } from '@angular/core';
import { DatePipe } from '@angular/common';
import { EventDto } from '../models/event.dto';

@Component({
  selector: 'app-event-timeline',
  imports: [DatePipe],
  templateUrl: './event-timeline.html',
})
export class EventTimeline {
  readonly events = input<EventDto[]>([]);
  readonly eventSelected = output<EventDto>();

  protected readonly hours = Array.from(
    { length: 24 },
    (_, index) => index
  );

  protected readonly timelineEvents = computed(() =>
    [...this.events()].sort(
      (a, b) =>
        new Date(a.eventDate).getTime() -
        new Date(b.eventDate).getTime()
    )
  );

  protected eventsAtHour(hour: number): EventDto[] {
    const selected = this.selectedDate();

    return this.timelineEvents().filter(event => {
      const eventDate = new Date(event.eventDate);

      const sameDay =
        eventDate.getFullYear() === selected.getFullYear() &&
        eventDate.getMonth() === selected.getMonth() &&
        eventDate.getDate() === selected.getDate();

      return sameDay && eventDate.getHours() === hour;
    });
  }

  protected selectEvent(event: EventDto): void {
    this.eventSelected.emit(event);
  }

  readonly selectedDate = input<Date>(new Date());
}
