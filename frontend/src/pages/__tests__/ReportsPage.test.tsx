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
  downloadHoursReportCsv: vi.fn()
}));

describe("ReportsPage", () => {
  it("shows the report lines with their hour type", async () => {
    render(<ReportsPage />);

    fireEvent.click(screen.getByRole("button", { name: "Run report" }));

    expect(await screen.findByText("Eva Employee")).toBeInTheDocument();
    expect(screen.getByText("Work")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Download CSV" })).toBeInTheDocument();
  });
});
