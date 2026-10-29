import { describe, expect, it } from "vitest";
import { isInLockedMonth, lockedMonthKeys, monthKey } from "../lockedMonths";

describe("lockedMonths", () => {
  it("builds month keys from a year and month or from a date", () => {
    expect(monthKey(2026, 9)).toBe("2026-09");
    expect(monthKey("2026-09-30")).toBe("2026-09");
  });

  it("knows which dates fall in a locked month", () => {
    const locked = lockedMonthKeys([
      { year: 2026, month: 9, lockedByName: "Bea", lockedAtUtc: "2026-10-03T08:00:00Z" },
      { year: 2025, month: 12, lockedByName: "Bea", lockedAtUtc: "2026-01-05T08:00:00Z" }
    ]);

    expect(isInLockedMonth(locked, "2026-09-01")).toBe(true);
    expect(isInLockedMonth(locked, "2026-09-30")).toBe(true);
    expect(isInLockedMonth(locked, "2026-10-01")).toBe(false);
    expect(isInLockedMonth(locked, "2025-12-31")).toBe(true);
    expect(isInLockedMonth(locked, "2026-12-01")).toBe(false);
  });
});
