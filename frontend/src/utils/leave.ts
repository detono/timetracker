import type { WorkScheduleDay } from "../types";

/** The context the app shell passes to its pages (via react-router's Outlet). */
export interface ShellContext {
  /** Leave requests waiting for the signed-in user's review. */
  pendingLeaveCount: number;
  refreshPendingLeaveCount: () => void;
}

export const WEEKDAYS = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"] as const;

/** "08:00:00" → "08:00", for time inputs. */
export function toHm(time: string): string {
  return time.slice(0, 5);
}

/** Net hours of a schedule day, or 0 when the times don't make sense yet. */
export function scheduleDayHours(day: Pick<WorkScheduleDay, "startTime" | "endTime" | "breakMinutes">): number {
  const minutes = (t: string) => {
    const [h, m] = t.split(":").map(Number);
    return h * 60 + (m || 0);
  };
  const net = minutes(day.endTime) - minutes(day.startTime) - (day.breakMinutes || 0);
  return net > 0 ? net / 60 : 0;
}

export function localizedName(names: Record<string, string>, language: string): string {
  const lang = language.split("-")[0];
  return names[lang] || names.en || Object.values(names)[0] || "—";
}
