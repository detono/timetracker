import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import "../../i18n";
import { ReportsPage } from "../ReportsPage";

vi.mock("../../auth/AuthContext", () => ({
  useAuth: () => ({ user: { role: "Employee", fullName: "Eva Employee" } })
}));

// Shaped exactly like the API's response: one hour type name, already in the caller's language.
vi.mock("../../api/reportsApi", () => ({
  getHoursReport: vi.fn().mockResolvedValue({
    grouping: "Week",
    from: "2026-10-01",
    to: "2026-10-29",
    grandTotalHours: 7.5,
    lines: [
      {
        userId: "u1",
        userFullName: "Eva Employee",
        hourTypeId: "h1",
        hourTypeName: "Work",
        hourTypeColor: "#932e4a",
        periodLabel: "2026-W43",
        periodStart: "2026-10-19",
        periodEnd: "2026-10-25",
        totalHours: 7.5,
        entryCount: 1
      }
    ]
  }),
  getHoursBalance: vi.fn().mockResolvedValue([
    {
      userId: "u1",
      userFullName: "Eva Employee",
      periodLabel: "2026-W43",
      periodStart: "2026-10-19",
      periodEnd: "2026-10-25",
      workedHours: 7.5,
      expectedHours: 37.5,
      differenceHours: -30
    }
  ]),
  downloadHoursReportCsv: vi.fn(),
  downloadHoursBalanceCsv: vi.fn()
}));

describe("ReportsPage", () => {
  it("shows the report lines with their hour type", async () => {
    render(<ReportsPage />);

    fireEvent.click(screen.getByRole("button", { name: "Run report" }));

    expect(await screen.findByText("Work")).toBeInTheDocument();
    expect(screen.getAllByText("Eva Employee")).toHaveLength(2);
    expect(screen.getByRole("button", { name: "Download CSV" })).toBeInTheDocument();
  });

  it("shows worked vs. expected per person", async () => {
    render(<ReportsPage />);

    fireEvent.click(screen.getByRole("button", { name: "Run report" }));

    expect(await screen.findByText("Worked vs. expected")).toBeInTheDocument();
    expect(screen.getByText("37h 30m")).toBeInTheDocument();
    expect(screen.getByText("−30h")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Download worked vs. expected (CSV)" })).toBeInTheDocument();
  });
});
