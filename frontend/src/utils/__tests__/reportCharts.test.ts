import { describe, expect, it } from "vitest";
import {
  billableShare,
  donutBackground,
  hourTypeTotals,
  periodTotals,
  personBalances,
  shortPeriodLabel
} from "../reportCharts";
import type { HoursBalanceLine, HoursReportLine, ProjectHoursLine } from "../../types";

const reportLine = (over: Partial<HoursReportLine>): HoursReportLine => ({
  userId: "u1",
  userFullName: "Eva",
  hourTypeId: "work",
  hourTypeName: "Work",
  hourTypeColor: "#932e4a",
  periodLabel: "2026-W43",
  periodStart: "2026-10-19",
  periodEnd: "2026-10-25",
  totalHours: 8,
  entryCount: 1,
  ...over
});

const balanceLine = (over: Partial<HoursBalanceLine>): HoursBalanceLine => ({
  userId: "u1",
  userFullName: "Eva",
  periodLabel: "2026-W43",
  periodStart: "2026-10-19",
  periodEnd: "2026-10-25",
  workedHours: 38,
  expectedHours: 40,
  differenceHours: -2,
  ...over
});

describe("periodTotals", () => {
  it("adds up everyone's worked and scheduled hours per period, in date order", () => {
    const totals = periodTotals(
      [],
      [
        balanceLine({ periodStart: "2026-10-26", periodEnd: "2026-11-01", workedHours: 40, expectedHours: 40 }),
        balanceLine({ workedHours: 38, expectedHours: 40 }),
        balanceLine({ userId: "u2", workedHours: 41, expectedHours: 40 })
      ]
    );

    expect(totals).toEqual([
      { start: "2026-10-19", end: "2026-10-25", worked: 79, expected: 80 },
      { start: "2026-10-26", end: "2026-11-01", worked: 40, expected: 40 }
    ]);
  });

  it("falls back to the logged hours, without a schedule, when there's no balance (a project filter)", () => {
    const totals = periodTotals(
      [reportLine({ totalHours: 3 }), reportLine({ hourTypeId: "travel", totalHours: 2 })],
      []
    );

    expect(totals).toEqual([{ start: "2026-10-19", end: "2026-10-25", worked: 5, expected: null }]);
  });
});

describe("hourTypeTotals", () => {
  it("adds up each hour type over people and periods, biggest first", () => {
    const totals = hourTypeTotals([
      reportLine({ hourTypeId: "travel", hourTypeName: "Travel", hourTypeColor: "#2f6f7d", totalHours: 2 }),
      reportLine({ totalHours: 8 }),
      reportLine({ userId: "u2", periodStart: "2026-10-26", totalHours: 7.5 })
    ]);

    expect(totals).toEqual([
      { id: "work", name: "Work", color: "#932e4a", hours: 15.5 },
      { id: "travel", name: "Travel", color: "#2f6f7d", hours: 2 }
    ]);
  });
});

describe("personBalances", () => {
  it("adds up each person's periods, sorted by name", () => {
    const people = personBalances([
      balanceLine({ userId: "u2", userFullName: "Zoe", differenceHours: 1 }),
      balanceLine({ differenceHours: -2 }),
      balanceLine({ periodStart: "2026-10-26", differenceHours: 0.5 })
    ]);

    expect(people.map((p) => [p.name, p.difference])).toEqual([
      ["Eva", -1.5],
      ["Zoe", 1]
    ]);
  });
});

describe("billableShare", () => {
  const line = (over: Partial<ProjectHoursLine>): ProjectHoursLine => ({
    projectId: "p1", projectName: "Website", clientName: null, isBillable: true, totalHours: 6, entryCount: 1, ...over
  });

  it("is the billable hours out of all hours, those without a project included", () => {
    expect(
      billableShare([line({}), line({ projectId: "p2", isBillable: false, totalHours: 2 }), line({ projectId: null, totalHours: 4 })])
    ).toEqual({ billable: 6, total: 12 });
  });

  it("is null when nobody logged on a project", () => {
    expect(billableShare([line({ projectId: null, isBillable: false })])).toBeNull();
  });
});

describe("donutBackground", () => {
  it("lays the parts one after another around the circle", () => {
    expect(donutBackground([{ value: 3, color: "red" }, { value: 1, color: "blue" }])).toBe(
      "conic-gradient(red 0% 75%, blue 75% 100%)"
    );
  });

  it("is an empty ring when there's nothing to show", () => {
    expect(donutBackground([], "grey")).toBe("conic-gradient(grey 0% 100%)");
  });
});

describe("shortPeriodLabel", () => {
  it("names weeks by their ISO number, months and days by date", () => {
    expect(shortPeriodLabel("2026-10-19", "Week", "en")).toBe("W43");
    expect(shortPeriodLabel("2026-10-01", "Month", "en")).toBe("Oct");
    expect(shortPeriodLabel("2026-10-19", "Day", "en")).toBe("19 Oct");
  });
});
