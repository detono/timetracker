import { addDays, format, startOfWeek } from "date-fns";

/** Returns the Monday..Sunday ISO date strings (yyyy-MM-dd) for the week containing `date`. */
export function getWeekRange(date: Date): { from: string; to: string; days: string[] } {
  const start = startOfWeek(date, { weekStartsOn: 1 });
  const days = Array.from({ length: 7 }, (_, i) => format(addDays(start, i), "yyyy-MM-dd"));
  return { from: days[0], to: days[6], days };
}

export function formatDayLabel(iso: string): string {
  return format(new Date(iso + "T00:00:00"), "EEE d MMM");
}

export function hoursToHm(hours: number): string {
  const h = Math.floor(hours);
  const m = Math.round((hours - h) * 60);
  return m === 0 ? `${h}h` : `${h}h ${m}m`;
}
