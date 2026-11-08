import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import { getHoursBalance, getHoursReport } from "../../api/reportsApi";
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
  getProjectHours: vi.fn().mockResolvedValue([
    { projectId: "p1", projectName: "Website", clientName: "Acme", isBillable: true, totalHours: 6, entryCount: 2 },
    { projectId: "p2", projectName: "Internal", clientName: null, isBillable: false, totalHours: 1.5, entryCount: 1 },
    { projectId: null, projectName: null, clientName: null, isBillable: false, totalHours: 2, entryCount: 1 }
  ]),
  downloadHoursReportCsv: vi.fn(),
  downloadHoursBalanceCsv: vi.fn(),
  downloadTimeEntriesCsv: vi.fn(),
  downloadProjectHoursCsv: vi.fn()
}));

vi.mock("../../api/projectsApi", () => ({
  projectsApi: {
    getAll: vi.fn().mockResolvedValue([
      { id: "p1", name: "Website", clientName: "Acme", isActive: true, totalHoursLifetime: 6, totalHoursThisMonth: 6,
        budgetHours: null, isBillable: true, memberIds: [], canLog: true }
    ])
  }
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

  it("shows the hours per project with how many are billable", async () => {
    render(<ReportsPage />);

    fireEvent.click(screen.getByRole("button", { name: "Run report" }));

    expect(await screen.findByText("Hours per project")).toBeInTheDocument();
    expect(screen.getByText("6h billable of 9h 30m in total.")).toBeInTheDocument();
    expect(screen.getByText("No project")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Download hours per project (CSV)" })).toBeInTheDocument();
  });

  it("limits the report to one project and leaves out worked vs. expected", async () => {
    vi.mocked(getHoursBalance).mockClear();
    render(<ReportsPage />);

    fireEvent.change(await screen.findByLabelText("Project"), { target: { value: "p1" } });
    fireEvent.click(screen.getByRole("button", { name: "Run report" }));

    expect(await screen.findByText("Hours per project")).toBeInTheDocument();
    expect(vi.mocked(getHoursReport)).toHaveBeenLastCalledWith(expect.any(String), expect.any(String), "Week", undefined, "p1");
    expect(getHoursBalance).not.toHaveBeenCalled();
    expect(screen.queryByText("Worked vs. expected")).not.toBeInTheDocument();
  });
});
