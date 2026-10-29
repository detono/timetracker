import type { Weekday, WorkSchedule } from "../types";

const WEEKDAYS: Weekday[] = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

export interface ScheduledTimes {
  startTime: string; // HH:mm
  endTime: string;
  breakMinutes: number;
}

/** The scheduled start, end and break for a date ("2026-10-30"), or null on a day off. */
export function scheduledTimes(schedule: WorkSchedule | null | undefined, isoDate: string): ScheduledTimes | null {
  if (!schedule || !/^\d{4}-\d{2}-\d{2}$/.test(isoDate)) return null;
  const weekday = WEEKDAYS[new Date(isoDate + "T00:00:00").getDay()];
  const day = schedule.days.find((d) => d.dayOfWeek === weekday);
  if (!day) return null;
  return { startTime: day.startTime.slice(0, 5), endTime: day.endTime.slice(0, 5), breakMinutes: day.breakMinutes };
}
