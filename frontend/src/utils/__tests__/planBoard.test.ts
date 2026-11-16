import { describe, expect, it } from "vitest";
import { buildRow, splitAtSchedule, stripRange } from "../planBoard";
import type { TimeEntry, WorkSchedule } from "../../types";

const week = ["2026-11-16", "2026-11-17", "2026-11-18", "2026-11-19", "2026-11-20", "2026-11-21", "2026-11-22"];

function entry(overrides: Partial<TimeEntry>): TimeEntry {
  return {
    id: Math.random().toString(36).slice(2),
    userId: "u1",
    userFullName: "Jonas Peeters",
    hourTypeId: "work",
    projectId: null,
    localizedHourTypeNames: { en: "Work" },
    hourTypeColor: "#1d9e75",
    workDate: "2026-11-16",
    startTime: "09:00:00",
    endTime: "17:00:00",
    breakMinutes: 0,
    durationHours: 8,
    notes: null,
    projectName: null,
    location: null,
    latitude: null,
    longitude: null,
    ...overrides
  };
}

const nineToFive: WorkSchedule = {
  userId: "u1",
  source: "Company",
  weeklyHours: 37.5,
  days: ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"].map((d) => ({
    dayOfWeek: d as WorkSchedule["days"][number]["dayOfWeek"],
    startTime: "09:00:00",
    endTime: "17:00:00",
    breakMinutes: 30
  }))
};

describe("buildRow", () => {
  it("adds up worked and scheduled hours for the week, skipping holidays", () => {
    const row = buildRow({
      days: week,
      entries: [entry({ workDate: "2026-11-16", durationHours: 8 }), entry({ workDate: "2026-11-17", durationHours: 6 })],
      schedule: nineToFive,
      holidays: { "2026-11-20": "Company day" },
      leaveHourTypeIds: new Set()
    });
    expect(row.workedHours).toBe(14);
    expect(row.expectedHours).toBe(30); // four scheduled days of 7h30
  });

  it("has no expected hours without a schedule", () => {
    const row = buildRow({ days: week, entries: [entry({})], schedule: null, holidays: {}, leaveHourTypeIds: new Set() });
    expect(row.expectedHours).toBeNull();
    const monday = row.items[0];
    expect(monday.kind === "day" && monday.expectedHours).toBeNull();
  });

  it("gives each day its schedule window and the difference with it", () => {
    const row = buildRow({
      days: week,
      entries: [entry({ workDate: "2026-11-16", startTime: "09:00:00", endTime: "15:00:00", durationHours: 6 })],
      schedule: nineToFive,
      holidays: {},
      leaveHourTypeIds: new Set()
    });
    const monday = row.items[0];
    expect(monday.kind).toBe("day");
    if (monday.kind !== "day") return;
    expect(monday.window).toEqual({ start: 9, end: 17 });
    expect(monday.workedHours).toBe(6);
    expect(monday.expectedHours).toBe(7.5);
  });

  it("merges consecutive days of the same leave into one span", () => {
    const leave = (date: string) =>
      entry({ workDate: date, hourTypeId: "leave", localizedHourTypeNames: { en: "Leave" }, durationHours: 7.5 });
    const row = buildRow({
      days: week,
      entries: [leave("2026-11-16"), leave("2026-11-17"), leave("2026-11-18"), entry({ workDate: "2026-11-19" })],
      schedule: nineToFive,
      holidays: {},
      leaveHourTypeIds: new Set(["leave"])
    });
    expect(row.items.map((i) => i.kind)).toEqual(["span", "day", "day", "day", "day"]);
    const span = row.items[0];
    if (span.kind !== "span") return;
    expect(span.days).toEqual(["2026-11-16", "2026-11-17", "2026-11-18"]);
    expect(span.workedHours).toBe(22.5);
    expect(span.entries).toHaveLength(3);
  });

  it("keeps a single day of leave, or leave mixed with work, as a normal day", () => {
    const row = buildRow({
      days: week,
      entries: [
        entry({ workDate: "2026-11-16", hourTypeId: "leave" }),
        entry({ workDate: "2026-11-18", hourTypeId: "leave", startTime: "09:00:00", endTime: "13:00:00" }),
        entry({ workDate: "2026-11-18", startTime: "13:00:00", endTime: "17:00:00" }),
        entry({ workDate: "2026-11-19", hourTypeId: "leave" })
      ],
      schedule: null,
      holidays: {},
      leaveHourTypeIds: new Set(["leave"])
    });
    expect(row.items.every((i) => i.kind === "day")).toBe(true);
    expect(row.items).toHaveLength(7);
  });

  it("sorts a day's entries by start time", () => {
    const row = buildRow({
      days: week,
      entries: [entry({ id: "late", startTime: "13:00:00" }), entry({ id: "early", startTime: "08:00:00" })],
      schedule: null,
      holidays: {},
      leaveHourTypeIds: new Set()
    });
    const monday = row.items[0];
    expect(monday.kind === "day" && monday.entries.map((e) => e.id)).toEqual(["early", "late"]);
  });
});

describe("splitAtSchedule", () => {
  it("splits an entry where it leaves the schedule", () => {
    expect(splitAtSchedule(7, 19.5, { start: 8.5, end: 17 })).toEqual([
      { start: 7, end: 8.5, outside: true },
      { start: 8.5, end: 17, outside: false },
      { start: 17, end: 19.5, outside: true }
    ]);
  });

  it("keeps an entry inside the schedule whole", () => {
    expect(splitAtSchedule(9, 12, { start: 8.5, end: 17 })).toEqual([{ start: 9, end: 12, outside: false }]);
  });

  it("marks everything outside on a day without a schedule window", () => {
    expect(splitAtSchedule(9, 12, null, true)).toEqual([{ start: 9, end: 12, outside: true }]);
  });

  it("marks nothing when the person has no schedule at all", () => {
    expect(splitAtSchedule(9, 12, null, false)).toEqual([{ start: 9, end: 12, outside: false }]);
  });
});

describe("stripRange", () => {
  it("defaults to 07:00-19:00", () => {
    expect(stripRange([], [])).toEqual({ start: 7, end: 19 });
  });

  it("widens to whole hours around early or late work", () => {
    expect(stripRange([entry({ startTime: "06:15:00", endTime: "21:30:00" })], [])).toEqual({ start: 6, end: 22 });
  });

  it("widens to fit a schedule", () => {
    expect(stripRange([], [{ start: 6.5, end: 15 }])).toEqual({ start: 6, end: 19 });
  });
});
