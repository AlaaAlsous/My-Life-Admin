import { describe, expect, it } from 'vitest';
import { EventDto } from '../models/event.dto';
import { addMonths, buildMonthGrid, groupEventsByDay, toDateKey } from './calendar.utils';

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

describe('calendar.utils', () => {
  it('formats a local date key', () => {
    expect(toDateKey(new Date(2026, 0, 5))).toBe('2026-01-05');
  });

  it('adds months across year boundaries', () => {
    const next = addMonths(new Date(2026, 11, 15), 1);
    expect(next.getFullYear()).toBe(2027);
    expect(next.getMonth()).toBe(0);
    expect(addMonths(new Date(2026, 0, 31), -1).getMonth()).toBe(11);
  });

  it('groups events per day, ordered by time, ignoring invalid dates', () => {
    const groups = groupEventsByDay([
      makeEvent(1, '2026-10-30T15:00:00'),
      makeEvent(2, '2026-10-30T09:00:00'),
      makeEvent(3, 'not-a-date'),
    ]);

    expect(groups.size).toBe(1);
    expect(groups.get('2026-10-30')?.map((e) => e.id)).toEqual([2, 1]);
  });

  it('builds whole Monday-first weeks for the month', () => {
    // October 2026 starts on a Thursday and has 31 days -> 5 weeks.
    const grid = buildMonthGrid(new Date(2026, 9, 1), [], new Date(2026, 9, 30));

    expect(grid.length).toBe(35);
    expect(grid[0].key).toBe('2026-09-28');
    expect(grid[0].inCurrentMonth).toBe(false);
    expect(grid[3].key).toBe('2026-10-01');
    expect(grid[3].inCurrentMonth).toBe(true);
    expect(grid[34].key).toBe('2026-11-01');
  });

  it('marks today and attaches events to the right day', () => {
    const grid = buildMonthGrid(
      new Date(2026, 9, 1),
      [makeEvent(1, '2026-10-30T10:00:00')],
      new Date(2026, 9, 30),
    );

    const today = grid.filter((day) => day.isToday);
    expect(today.length).toBe(1);
    expect(today[0].key).toBe('2026-10-30');
    expect(today[0].events.map((e) => e.id)).toEqual([1]);
    expect(grid.filter((day) => day.events.length > 0).length).toBe(1);
  });

  it('uses 6 weeks when the month spans them', () => {
    // March 2026 starts on a Sunday and has 31 days -> 6 weeks.
    expect(buildMonthGrid(new Date(2026, 2, 1), []).length).toBe(42);
  });
});
