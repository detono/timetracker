import { getISOWeek, parseISO } from "date-fns";
import type { HoursBalanceLine, HoursReportLine, ProjectHoursLine, ReportGrouping } from "../types";
import { dateLocale } from "./dateRange";

/** Everyone's hours in one period; `expected` is null when the schedule isn't known (a project filter). */
export interface PeriodTotal {
  start: string;
  end: string;
  worked: number;
  expected: number | null;
}

/**
 * Worked vs. scheduled per period, everyone together. The balance has both; without it (a
 * project filter leaves it out) the logged hours are all there is.
 */
export function periodTotals(lines: HoursReportLine[], balance: HoursBalanceLine[]): PeriodTotal[] {
  const byStart = new Map<string, PeriodTotal>();

  if (balance.length > 0) {
    for (const line of balance) {
      const total = byStart.get(line.periodStart) ?? { start: line.periodStart, end: line.periodEnd, worked: 0, expected: 0 };
      total.worked += line.workedHours;
      total.expected = (total.expected ?? 0) + line.expectedHours;
      byStart.set(line.periodStart, total);
    }
  } else {
    for (const line of lines) {
      const total = byStart.get(line.periodStart) ?? { start: line.periodStart, end: line.periodEnd, worked: 0, expected: null };
      total.worked += line.totalHours;
      byStart.set(line.periodStart, total);
    }
  }

  return [...byStart.values()].sort((a, b) => a.start.localeCompare(b.start));
}

export interface HourTypeTotal {
  id: string;
  name: string;
  color: string;
  hours: number;
}

/** Hours per hour type over everyone and every period, biggest first. */
export function hourTypeTotals(lines: HoursReportLine[]): HourTypeTotal[] {
  const byType = new Map<string, HourTypeTotal>();
  for (const line of lines) {
    const total = byType.get(line.hourTypeId) ?? { id: line.hourTypeId, name: line.hourTypeName, color: line.hourTypeColor, hours: 0 };
    total.hours += line.totalHours;
    byType.set(line.hourTypeId, total);
  }
  return [...byType.values()].sort((a, b) => b.hours - a.hours);
}

export interface PersonBalance {
  userId: string;
  name: string;
  worked: number;
  expected: number;
  /** Worked minus expected: negative means behind. */
  difference: number;
}

/** Each person's worked vs. expected over all periods, sorted by name. */
export function personBalances(balance: HoursBalanceLine[]): PersonBalance[] {
  const byUser = new Map<string, PersonBalance>();
  for (const line of balance) {
    const person = byUser.get(line.userId) ?? { userId: line.userId, name: line.userFullName, worked: 0, expected: 0, difference: 0 };
    person.worked += line.workedHours;
    person.expected += line.expectedHours;
    person.difference += line.differenceHours;
    byUser.set(line.userId, person);
  }
  return [...byUser.values()].sort((a, b) => a.name.localeCompare(b.name));
}

/** Billable hours out of all hours (also those without a project); null when nobody logged on a project. */
export function billableShare(lines: ProjectHoursLine[]): { billable: number; total: number } | null {
  if (!lines.some((l) => l.projectId !== null)) return null;
  const total = lines.reduce((sum, l) => sum + l.totalHours, 0);
  const billable = lines.filter((l) => l.projectId && l.isBillable).reduce((sum, l) => sum + l.totalHours, 0);
  return { billable, total };
}

/** A CSS conic gradient with the parts one after another; the page lays a disc on top for the hole. */
export function donutBackground(parts: { value: number; color: string }[], emptyColor = "var(--line)"): string {
  const total = parts.reduce((sum, p) => sum + Math.max(p.value, 0), 0);
  if (total <= 0) return `conic-gradient(${emptyColor} 0% 100%)`;

  let at = 0;
  const stops = parts.map((p) => {
    const from = at;
    at += (Math.max(p.value, 0) / total) * 100;
    return `${p.color} ${round(from)}% ${round(at)}%`;
  });
  return `conic-gradient(${stops.join(", ")})`;
}

const round = (n: number) => Math.round(n * 100) / 100;

/** A label short enough to sit under a bar: "W43", "Oct", "19 Oct". */
export function shortPeriodLabel(start: string, grouping: ReportGrouping, language?: string): string {
  const date = parseISO(start);
  if (grouping === "Week") return `W${getISOWeek(date)}`;
  const locale = dateLocale(language);
  return grouping === "Month"
    ? date.toLocaleDateString(locale, { month: "short" })
    : date.toLocaleDateString(locale, { day: "numeric", month: "short" });
}
