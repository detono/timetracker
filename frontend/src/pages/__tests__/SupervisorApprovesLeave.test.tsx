import { describe, expect, it, vi } from "vitest";
import { act, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import "../../i18n";
import { EmployeesPage } from "../EmployeesPage";
import { ConfirmProvider } from "../../components/ConfirmDialog";
import { assignSupervisor } from "../../api/usersApi";
import type { UserAccount } from "../../types";

vi.mock("../../auth/AuthContext", () => ({
  useAuth: () => ({ user: { userId: "boss", role: "Employer", fullName: "Jonathan Boss" } })
}));

const person = (id: string, firstName: string, extra: Partial<UserAccount> = {}): UserAccount => ({
  id, firstName, lastName: "Tono", email: `${id}@co.test`, role: "Employee", supervisorId: null,
  isActive: true, tracksHours: true, ...extra
});

vi.mock("../../api/usersApi", () => ({
  getAllUsers: vi.fn(async () => [
    person("marcella", "Marcella"),
    person("luz", "Luz", { supervisorId: "marcella", supervisorApprovesLeave: true })
  ]),
  assignSupervisor: vi.fn(async () => undefined),
  activateUser: vi.fn(),
  createUser: vi.fn(),
  deactivateUser: vi.fn(),
  resetPassword: vi.fn(),
  setExternalId: vi.fn(),
  setTracksHours: vi.fn()
}));

describe("Supervisor approves leave", () => {
  it("shows the option only with a supervisor, and unticking keeps the supervisor", async () => {
    render(
      <MemoryRouter>
        <ConfirmProvider>
          <EmployeesPage />
        </ConfirmProvider>
      </MemoryRouter>
    );

    const checkboxes = await screen.findAllByRole("checkbox", { name: "Also approves leave" });
    expect(checkboxes).toHaveLength(1); // Marcella has no supervisor
    expect(checkboxes[0]).toBeChecked();

    await act(async () => fireEvent.click(checkboxes[0]));
    expect(assignSupervisor).toHaveBeenCalledWith("luz", "marcella", false);
  });
});
