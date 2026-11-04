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
    { length: 13 },
    (_, index) => index + 8
  );

  protected readonly timelineEvents = computed(() =>
    [...this.events()].sort(
      (a, b) =>
        new Date(a.eventDate).getTime() -
        new Date(b.eventDate).getTime()
    )
  );

  protected eventsAtHour(hour: number): EventDto[] {
    return this.timelineEvents().filter(
      event => new Date(event.eventDate).getHours() === hour
    );
  }

  protected selectEvent(event: EventDto): void {
    this.eventSelected.emit(event);
  }

  readonly selectedDate = input<Date>(new Date());
}
