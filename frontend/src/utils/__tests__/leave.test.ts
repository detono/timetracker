import { describe, expect, it } from "vitest";
import { localizedName, scheduleDayHours, toHm } from "../leave";

describe("scheduleDayHours", () => {
  it("subtracts the break", () => {
    expect(scheduleDayHours({ startTime: "09:00", endTime: "17:00", breakMinutes: 30 })).toBe(7.5);
  });

  it("accepts times with seconds", () => {
    expect(scheduleDayHours({ startTime: "08:00:00", endTime: "12:30:00", breakMinutes: 0 })).toBe(4.5);
  });

  it("returns 0 for an invalid day instead of a negative number", () => {
    expect(scheduleDayHours({ startTime: "17:00", endTime: "09:00", breakMinutes: 0 })).toBe(0);
  });
});

describe("toHm", () => {
  it("drops the seconds", () => {
    expect(toHm("08:30:00")).toBe("08:30");
  });
});

describe("localizedName", () => {
  it("prefers the UI language, then English", () => {
    const names = { en: "Paid time off", nl: "Verlof" };
    expect(localizedName(names, "nl-BE")).toBe("Verlof");
    expect(localizedName(names, "fr")).toBe("Paid time off");
  });
});
