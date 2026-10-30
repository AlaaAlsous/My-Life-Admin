import { ComponentFixture, TestBed } from '@angular/core/testing';
import { beforeEach, describe, expect, it } from 'vitest';
import { EventDto } from '../models/event.dto';
import { EventCalendar } from './event-calendar';

function makeEvent(id: number, eventDate: string): EventDto {
  return {
    id,
    userId: 1,
    documentId: null,
    title: `Event ${id}`,
    description: null,
    category: 'Insurance',
    eventDate,
    createdAt: '2026-01-01T00:00:00',
  };
}

describe('EventCalendar', () => {
  let fixture: ComponentFixture<EventCalendar>;
  let element: HTMLElement;

  function setup(events: EventDto[], month = new Date(2026, 9, 1)): void {
    fixture.componentRef.setInput('events', events);
    fixture.componentRef.setInput('month', month);
    fixture.detectChanges();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [EventCalendar] });
    fixture = TestBed.createComponent(EventCalendar);
    element = fixture.nativeElement;
  });

  it('renders the month header, 7 weekday columns and one cell per day', () => {
    setup([]);

    expect(element.querySelector('h2')?.textContent).toContain('October 2026');
    expect(element.querySelectorAll('[role="button"]').length).toBe(35);
    expect(element.textContent).toContain('Mon');
    expect(element.textContent).toContain('Sun');
  });

  it('shows an empty state but keeps the calendar when there are no events', () => {
    setup([]);

    expect(element.textContent).toContain('No events this month.');
    expect(element.querySelectorAll('[role="button"]').length).toBe(35);
  });

  it('shows event titles in the day cell', () => {
    setup([makeEvent(1, '2026-10-30T10:00:00')]);

    expect(element.textContent).not.toContain('No events this month.');
    expect(element.textContent).toContain('Event 1');
  });

  it('shows at most two events per day and a +N more indicator', () => {
    setup([
      makeEvent(1, '2026-10-30T08:00:00'),
      makeEvent(2, '2026-10-30T09:00:00'),
      makeEvent(3, '2026-10-30T10:00:00'),
      makeEvent(4, '2026-10-30T11:00:00'),
    ]);

    expect(element.querySelectorAll('li button').length).toBe(2);
    expect(element.textContent).toContain('+2 more');
  });

  it('emits previous and next months', () => {
    setup([]);
    const emitted: Date[] = [];
    fixture.componentInstance.monthChange.subscribe((value) => emitted.push(value));

    (element.querySelector('[aria-label="Previous month"]') as HTMLButtonElement).click();
    (element.querySelector('[aria-label="Next month"]') as HTMLButtonElement).click();

    expect(emitted[0].getMonth()).toBe(8);
    expect(emitted[1].getMonth()).toBe(10);
  });

  it('emits the current month when Today is clicked', () => {
    setup([]);
    const emitted: Date[] = [];
    fixture.componentInstance.monthChange.subscribe((value) => emitted.push(value));

    const todayButton = Array.from(element.querySelectorAll('button')).find(
      (button) => button.textContent?.trim() === 'Today',
    ) as HTMLButtonElement;
    todayButton.click();

    const now = new Date();
    expect(emitted[0].getMonth()).toBe(now.getMonth());
    expect(emitted[0].getFullYear()).toBe(now.getFullYear());
  });

  it('emits eventSelected without also selecting the day', () => {
    setup([makeEvent(1, '2026-10-30T10:00:00')]);
    const selectedEvents: EventDto[] = [];
    let daySelections = 0;
    fixture.componentInstance.eventSelected.subscribe((value) => selectedEvents.push(value));
    fixture.componentInstance.daySelected.subscribe(() => daySelections++);

    (element.querySelector('li button') as HTMLButtonElement).click();

    expect(selectedEvents.map((e) => e.id)).toEqual([1]);
    expect(daySelections).toBe(0);
  });

  it('emits daySelected when a day cell is clicked', () => {
    setup([]);
    const selected: string[] = [];
    fixture.componentInstance.daySelected.subscribe((day) => selected.push(day.key));

    (element.querySelectorAll('[role="button"]')[3] as HTMLElement).click();

    expect(selected).toEqual(['2026-10-01']);
  });
});
