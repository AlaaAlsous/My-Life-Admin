import { Component, OnInit, inject } from '@angular/core';
import { EventDto } from '../models/event.dto';
import { EventService } from './event.service';

@Component({
  selector: 'app-event',
  standalone: true,
  templateUrl: './event.component.html',
})
export class EventComponent implements OnInit {
  private readonly eventService = inject(EventService);

  events: EventDto[] = [];
  isLoading = false;
  errorMessage = '';

  ngOnInit(): void {
    this.loadEvents();
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
}
