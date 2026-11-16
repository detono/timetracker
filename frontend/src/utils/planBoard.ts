import type { TimeEntry, WorkSchedule } from "../types";
import { scheduledTimes } from "./workSchedule";

/** Hours of the day as a number: 8.5 is 08:30. */
export interface HourRange {
  start: number;
  end: number;
}

export interface DayItem {
  kind: "day";
  date: string;
  /** Sorted by start time. */
  entries: TimeEntry[];
  workedHours: number;
  /** Scheduled hours that day (0 on a day off or holiday); null when the person has no schedule. */
  expectedHours: number | null;
  /** When the person is scheduled to work that day; null on a day off, a holiday, or without a schedule. */
  window: HourRange | null;
}

/** Two or more days in a row of the same leave, drawn as one bar. */
export interface SpanItem {
  kind: "span";
  days: string[];
  entries: TimeEntry[];
  workedHours: number;
  expectedHours: number | null;
}

export interface PlanBoardRow {
  workedHours: number;
  /** Scheduled hours for the days in view; null when the person has no schedule. */
  expectedHours: number | null;
  items: (DayItem | SpanItem)[];
}

/** "08:30:00" → 8.5 */
export function timeToHours(time: string): number {
  const [h, m] = time.split(":").map(Number);
  return h + (m || 0) / 60;
}

function scheduleFor(schedule: WorkSchedule | null, date: string, holidays: Record<string, string>) {
  if (!schedule) return { window: null, expected: null };
  const times = holidays[date] ? null : scheduledTimes(schedule, date);
  if (!times) return { window: null, expected: 0 };
  const start = timeToHours(times.startTime);
  const end = timeToHours(times.endTime);
  return { window: { start, end }, expected: Math.max(0, end - start - times.breakMinutes / 60) };
}

const sumHours = (entries: TimeEntry[]) => entries.reduce((total, e) => total + e.durationHours, 0);

/** The one leave hour type a whole day was booked on, or null when it holds anything else. */
function leaveTypeOf(entries: TimeEntry[], leaveHourTypeIds: ReadonlySet<string>): string | null {
  if (entries.length === 0) return null;
  const type = entries[0].hourTypeId;
  return leaveHourTypeIds.has(type) && entries.every((e) => e.hourTypeId === type) ? type : null;
}

/**
 * Lays out one person's week: a cell per day, except that days in a row booked entirely on
 * the same leave become one span.
 */
export function buildRow({
  days,
  entries,
  schedule,
  holidays,
  leaveHourTypeIds
}: {
  days: string[];
  entries: TimeEntry[];
  schedule: WorkSchedule | null;
  holidays: Record<string, string>;
  leaveHourTypeIds: ReadonlySet<string>;
}): PlanBoardRow {
  const dayItems: DayItem[] = days.map((date) => {
    const dayEntries = entries
      .filter((e) => e.workDate === date)
      .sort((a, b) => a.startTime.localeCompare(b.startTime));
    const { window, expected } = scheduleFor(schedule, date, holidays);
    return { kind: "day", date, entries: dayEntries, workedHours: sumHours(dayEntries), expectedHours: expected, window };
  });

  const items: (DayItem | SpanItem)[] = [];
  for (let i = 0; i < dayItems.length; ) {
    const type = leaveTypeOf(dayItems[i].entries, leaveHourTypeIds);
    let end = i;
    while (type && end + 1 < dayItems.length && leaveTypeOf(dayItems[end + 1].entries, leaveHourTypeIds) === type) end++;
    if (end > i) {
      const run = dayItems.slice(i, end + 1);
      items.push({
        kind: "span",
        days: run.map((d) => d.date),
        entries: run.flatMap((d) => d.entries),
        workedHours: run.reduce((total, d) => total + d.workedHours, 0),
        expectedHours: schedule ? run.reduce((total, d) => total + (d.expectedHours ?? 0), 0) : null
      });
    } else {
      items.push(dayItems[i]);
    }
    i = end + 1;
  }

  return {
    workedHours: sumHours(entries.filter((e) => days.includes(e.workDate))),
    expectedHours: schedule ? dayItems.reduce((total, d) => total + (d.expectedHours ?? 0), 0) : null,
    items
  };
}

/**
 * Cuts an entry where it crosses the edges of the schedule window, marking the parts outside
 * it. Without a window, everything is outside when the person has a schedule (a day off or a
 * holiday) and nothing is when they don't.
 */
export function splitAtSchedule(
  start: number,
  end: number,
  window: HourRange | null,
  hasSchedule: boolean = window !== null
): { start: number; end: number; outside: boolean }[] {
  if (!window) return [{ start, end, outside: hasSchedule }];
  const cuts = [start, ...[window.start, window.end].filter((c) => c > start && c < end), end];
  return cuts.slice(0, -1).map((from, i) => {
    const to = cuts[i + 1];
    const middle = (from + to) / 2;
    return { start: from, end: to, outside: middle < window.start || middle > window.end };
  });
}

/** The hours the time strips cover: 07:00-19:00, widened to whole hours to fit everything shown. */
export function stripRange(entries: TimeEntry[], windows: HourRange[]): HourRange {
  let start = 7;
  let end = 19;
  for (const e of entries) {
    start = Math.min(start, timeToHours(e.startTime));
    end = Math.max(end, timeToHours(e.endTime));
  }
  for (const w of windows) {
    start = Math.min(start, w.start);
    end = Math.max(end, w.end);
  }
  return { start: Math.floor(start), end: Math.min(24, Math.ceil(end)) };
}
