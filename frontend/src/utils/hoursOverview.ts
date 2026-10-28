import { format } from "date-fns";
import { hoursToHm } from "./dateRange";

export type DiffTone = "plus" | "even" | "short" | "behind";

/** Differences smaller than this (in hours) count as on target. */
const TOLERANCE = 0.25;

/** Today in the user's own time zone, as yyyy-MM-dd. */
export function localToday(): string {
  return format(new Date(), "yyyy-MM-dd");
}

/**
 * How a difference reads: over, on target, a little short, or more than a working day
 * behind (a day being a fifth of the weekly schedule).
 */
export function diffTone(differenceHours: number, weeklyScheduleHours: number): DiffTone {
  if (differenceHours >= TOLERANCE) return "plus";
  if (differenceHours > -TOLERANCE) return "even";
  const day = weeklyScheduleHours > 0 ? weeklyScheduleHours / 5 : 8;
  return differenceHours <= -day ? "behind" : "short";
}

/** Like hoursToHm, but "30m" instead of "0h 30m" for less than an hour. */
export function shortHm(hours: number): string {
  const abs = Math.abs(hours);
  return abs > 0 && abs < 1 ? `${Math.round(abs * 60)}m` : hoursToHm(abs);
}

/** "+1h 30m", "+30m", "−2h", or "0h". */
export function signedHm(hours: number): string {
  if (Math.abs(hours) < 1 / 120) return "0h";
  return `${hours > 0 ? "+" : "−"}${shortHm(hours)}`;
}
