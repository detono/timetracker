import type { LockedMonth } from "../types";

/** "2026-09" for September 2026; also accepts a "yyyy-MM-dd" date. */
export function monthKey(yearOrDate: number | string, month?: number): string {
  if (typeof yearOrDate === "string") return yearOrDate.slice(0, 7);
  return `${yearOrDate}-${String(month).padStart(2, "0")}`;
}

/** The locked months as a set of month keys, for quick lookups by date. */
export function lockedMonthKeys(months: LockedMonth[]): Set<string> {
  return new Set(months.map((m) => monthKey(m.year, m.month)));
}

export function isInLockedMonth(locked: Set<string>, isoDate: string): boolean {
  return locked.has(monthKey(isoDate));
}
