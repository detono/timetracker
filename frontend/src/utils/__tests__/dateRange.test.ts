import { describe, expect, it } from "vitest";
import { getWeekRange, hoursToHm } from "../dateRange";

describe("hoursToHm", () => {
  it("formats whole hours", () => {
    expect(hoursToHm(8)).toBe("8h");
  });

  it("formats hours with minutes", () => {
    expect(hoursToHm(7.5)).toBe("7h 30m");
  });

  it("rounds minutes", () => {
    expect(hoursToHm(1.99)).toBe("1h 59m");
  });
});

describe("getWeekRange", () => {
  it("returns a Monday-to-Sunday range containing the given date", () => {
    const wednesday = new Date("2026-01-07T12:00:00Z");
    const { from, to, days } = getWeekRange(wednesday);

    expect(from).toBe("2026-01-05");
    expect(to).toBe("2026-01-11");
    expect(days).toHaveLength(7);
  });
});
