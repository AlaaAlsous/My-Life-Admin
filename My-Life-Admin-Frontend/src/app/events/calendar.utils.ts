import { EventDto } from '../models/event.dto';

export interface CalendarDay {
  date: Date;
  key: string;
  inCurrentMonth: boolean;
  isToday: boolean;
  events: EventDto[];
}

export const WEEKDAY_LABELS = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

export function toDateKey(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${date.getFullYear()}-${month}-${day}`;
}

export function addMonths(date: Date, months: number): Date {
  return new Date(date.getFullYear(), date.getMonth() + months, 1);
}

// Groups events by the local calendar day of their eventDate.
export function groupEventsByDay(events: EventDto[]): Map<string, EventDto[]> {
  const groups = new Map<string, EventDto[]>();

  for (const event of events) {
    const date = new Date(event.eventDate);

    if (Number.isNaN(date.getTime())) {
      continue;
    }

    const key = toDateKey(date);
    const list = groups.get(key) ?? [];
    list.push(event);
    groups.set(key, list);
  }

  for (const list of groups.values()) {
    list.sort((a, b) => new Date(a.eventDate).getTime() - new Date(b.eventDate).getTime());
  }

  return groups;
}

// Builds full weeks (Monday first) covering the month of `month`.
export function buildMonthGrid(
  month: Date,
  events: EventDto[],
  today: Date = new Date(),
): CalendarDay[] {
  const firstOfMonth = new Date(month.getFullYear(), month.getMonth(), 1);
  const offset = (firstOfMonth.getDay() + 6) % 7;
  const daysInMonth = new Date(month.getFullYear(), month.getMonth() + 1, 0).getDate();
  const totalCells = Math.ceil((offset + daysInMonth) / 7) * 7;
  const grouped = groupEventsByDay(events);
  const todayKey = toDateKey(today);

  return Array.from({ length: totalCells }, (_, index) => {
    const date = new Date(firstOfMonth.getFullYear(), firstOfMonth.getMonth(), index - offset + 1);
    const key = toDateKey(date);

    return {
      date,
      key,
      inCurrentMonth: date.getMonth() === firstOfMonth.getMonth(),
      isToday: key === todayKey,
      events: grouped.get(key) ?? [],
    };
  });
}
