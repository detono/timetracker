import { beforeEach, describe, expect, it, vi } from "vitest";
import { act, fireEvent, render, screen } from "@testing-library/react";
import "../../i18n";
import { ProjectsPage } from "../ProjectsPage";
import { projectsApi } from "../../api/projectsApi";

const project = {
  id: "p1", name: "Website", clientName: "Acme", isActive: true, totalHoursLifetime: 132, totalHoursThisMonth: 12,
  budgetHours: 120, isBillable: true, memberIds: ["u1", "u2"], canLog: false
};

vi.mock("../../api/projectsApi", () => ({
  projectsApi: {
    getAll: vi.fn(),
    create: vi.fn().mockResolvedValue("p2"),
    update: vi.fn().mockResolvedValue(undefined),
    toggleStatus: vi.fn(),
    getBreakdown: vi.fn().mockResolvedValue([{ userId: "u1", employeeName: "Mia Member", totalHours: 2.5 }])
  }
}));

vi.mock("../../api/usersApi", () => ({
  getAllUsers: vi.fn().mockResolvedValue([
    { id: "u1", firstName: "Mia", lastName: "Member", email: "", role: "Employee", supervisorId: null, isActive: true, tracksHours: true },
    { id: "u2", firstName: "Otto", lastName: "Other", email: "", role: "Employee", supervisorId: null, isActive: true, tracksHours: true },
    { id: "u3", firstName: "Lea", lastName: "Left", email: "", role: "Employee", supervisorId: null, isActive: false, tracksHours: true }
  ])
}));

describe("ProjectsPage", () => {
  beforeEach(() => {
    vi.mocked(projectsApi.getAll).mockResolvedValue([project]);
    vi.mocked(projectsApi.create).mockClear();
  });

  it("shows the budget used, billable and who can log", async () => {
    render(<ProjectsPage />);

    expect(await screen.findByText("110% of 120h")).toBeInTheDocument();
    expect(screen.getByText("110% of 120h").closest(".budget")).toHaveClass("budget--over");
    expect(screen.getByText("Billable · Only 2 people")).toBeInTheDocument();
  });

  it("creates a project for chosen people only, and needs at least one", async () => {
    render(<ProjectsPage />);
    await screen.findByText("Website");

    fireEvent.change(screen.getByLabelText("Project name"), { target: { value: "Shop" } });
    fireEvent.change(screen.getByLabelText("Budget (hours)"), { target: { value: "40" } });
    fireEvent.click(screen.getByLabelText("Billable"));
    fireEvent.click(screen.getByLabelText("Only these people"));
    expect(screen.queryByLabelText(/Lea Left/)).not.toBeInTheDocument();

    await act(async () => fireEvent.click(screen.getByRole("button", { name: "Create project" })));
    expect(screen.getByText("Choose at least one person, or let everyone log hours on it.")).toBeInTheDocument();
    expect(projectsApi.create).not.toHaveBeenCalled();

    fireEvent.click(screen.getByLabelText("Otto Other"));
    await act(async () => fireEvent.click(screen.getByRole("button", { name: "Create project" })));
    expect(projectsApi.create).toHaveBeenCalledWith({
      name: "Shop", clientName: null, budgetHours: 40, isBillable: true, memberIds: ["u2"]
    });
  });

  it("filters the breakdown on dates", async () => {
    render(<ProjectsPage />);
    await screen.findByText("Website");

    fireEvent.click(screen.getByRole("button", { name: "Actions" }));
    fireEvent.click(screen.getByRole("menuitem", { name: "View details" }));
    expect(await screen.findByText("Mia Member")).toBeInTheDocument();
    expect(projectsApi.getBreakdown).toHaveBeenLastCalledWith("p1", "", "");

    fireEvent.change(screen.getByLabelText("From"), { target: { value: "2026-03-01" } });
    expect(projectsApi.getBreakdown).toHaveBeenLastCalledWith("p1", "2026-03-01", "");
    await screen.findByText("Mia Member");
    fireEvent.click(screen.getByRole("button", { name: "All time" }));
    expect(projectsApi.getBreakdown).toHaveBeenLastCalledWith("p1", "", "");
    await screen.findByText("Mia Member");
  });
});
