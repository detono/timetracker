import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import "../../i18n";
import { ReportCharts } from "../ReportCharts";
import type { HoursBalanceLine, HoursReport, LeaveBalance } from "../../types";

const report: HoursReport = {
  grouping: "Week",
  from: "2026-11-01",
  to: "2026-11-16",
  grandTotalHours: 8,
  lines: [
    {
      userId: "u1", userFullName: "Luz", hourTypeId: "h1", hourTypeName: "Work", hourTypeColor: "#932e4a",
      periodLabel: "2026-W46", periodStart: "2026-11-09", periodEnd: "2026-11-15", totalHours: 8, entryCount: 1
    }
  ]
};

const balance = (over: Partial<HoursBalanceLine>): HoursBalanceLine => ({
  userId: "u1", userFullName: "Luz", periodLabel: "2026-W44", periodStart: "2026-11-01", periodEnd: "2026-11-01",
  workedHours: 0, expectedHours: 0, differenceHours: 0, ...over
});

const leave = (over: Partial<LeaveBalance>): LeaveBalance => ({
  userId: "u1", userFullName: "Luz", year: 2026, allowanceHours: 40, usedHours: 23, pendingHours: 0, remainingHours: 17, ...over
});

function renderCharts(balanceLines: HoursBalanceLine[], leaveLines: LeaveBalance[]) {
  render(<ReportCharts report={report} balance={balanceLines} projectHours={[]} leave={leaveLines} leaveYear={2026} />);
}

describe("ReportCharts", () => {
  it("says how far someone went over their leave allowance", () => {
    renderCharts([], [leave({}), leave({ userId: "u2", userFullName: "Marcella", usedHours: 48, remainingHours: -8 })]);

    expect(screen.getByText("17h left")).toBeInTheDocument();
    expect(screen.getByText("8h over the allowance")).toBeInTheDocument();
  });

  it("shows one's own leave over the allowance as a minus", () => {
    renderCharts([], [leave({ usedHours: 48, remainingHours: -8 })]);

    expect(screen.getByRole("img", { name: "Leave in 2026" })).toHaveTextContent("−8hover");
  });

  it("draws no schedule line for a period with nothing scheduled", () => {
    const { container } = render(
      <ReportCharts report={report} leave={null} leaveYear={2026} projectHours={[]}
        balance={[balance({}), balance({ periodLabel: "2026-W46", periodStart: "2026-11-09", periodEnd: "2026-11-15", workedHours: 8, expectedHours: 40, differenceHours: -32 })]} />
    );

    expect(container.querySelectorAll(".report-charts__target")).toHaveLength(1);
  });
});
