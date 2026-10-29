import i18next from "i18next";
import { addDays, format, startOfWeek } from "date-fns";

/** Returns the Monday..Sunday ISO date strings (yyyy-MM-dd) for the week containing `date`. */
export function getWeekRange(date: Date): { from: string; to: string; days: string[] } {
  const start = startOfWeek(date, { weekStartsOn: 1 });
  const days = Array.from({ length: 7 }, (_, i) => format(addDays(start, i), "yyyy-MM-dd"));
  return { from: days[0], to: days[6], days };
}

/**
 * "Wed 4 Nov" in the app's current language ("wo 4 nov", "mer. 4 nov.", "Mi., 4. Nov.").
 * English uses the British day-month order, like the rest of the app.
 */
export function formatDayLabel(iso: string, language: string = i18next.language || "en"): string {
  const locale = dateLocale(language);
  return new Date(iso + "T00:00:00").toLocaleDateString(locale, { weekday: "short", day: "numeric", month: "short" });
}

export function hoursToHm(hours: number): string {
  const h = Math.floor(hours);
  const m = Math.round((hours - h) * 60);
  return m === 0 ? `${h}h` : `${h}h ${m}m`;
}

/** The locale for dates: English uses the British day-month order, like the rest of the app. */
export function dateLocale(language: string = i18next.language || "en"): string {
  return language === "en" ? "en-GB" : language;
}
