import { useEffect, useState } from "react";
import { getPublicHolidays } from "../api/companyApi";

/**
 * The company's public holidays and days off between `from` and `to` (yyyy-MM-dd),
 * as date → name. A week around New Year spans two years, so both are fetched.
 * Holidays only add context to the page, so a failed load just shows none.
 */
export function usePublicHolidays(from: string, to: string): Record<string, string> {
  const [holidays, setHolidays] = useState<Record<string, string>>({});

  useEffect(() => {
    let cancelled = false;
    const years = Array.from(new Set([Number(from.slice(0, 4)), Number(to.slice(0, 4))]));
    Promise.all(years.map(getPublicHolidays))
      .then((lists) => {
        if (cancelled) return;
        const inRange: Record<string, string> = {};
        for (const holiday of lists.flat()) {
          if (holiday.date >= from && holiday.date <= to) inRange[holiday.date] = holiday.name;
        }
        setHolidays(inRange);
      })
      .catch(() => {
        if (!cancelled) setHolidays({});
      });
    return () => {
      cancelled = true;
    };
  }, [from, to]);

  return holidays;
}
