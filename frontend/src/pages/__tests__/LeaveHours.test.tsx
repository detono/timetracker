import { beforeEach, describe, expect, it, vi } from "vitest";
import { act, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import "../../i18n";
import { LeavePage } from "../LeavePage";
import { ConfirmProvider } from "../../components/ConfirmDialog";
import { createLeaveRequest, previewLeaveRequest } from "../../api/leaveApi";

vi.mock("../../auth/AuthContext", () => ({
  useAuth: () => ({ user: { userId: "eva", role: "Employee", fullName: "Eva Peeters" } })
}));

vi.mock("../../api/hourTypesApi", () => ({
  getHourTypes: vi.fn().mockResolvedValue([
    {
      id: "pto", localizedNames: { en: "Paid time off" }, colorHex: "#2a7ab0", isActive: true, isDefault: false,
      isLeave: true, deductsFromLeaveBalance: true
    }
  ])
}));

vi.mock("../../api/leaveApi", () => ({
  getLeaveRequests: vi.fn().mockResolvedValue([]),
  getLeaveBalances: vi.fn().mockResolvedValue([]),
  approveLeaveRequest: vi.fn(),
  rejectLeaveRequest: vi.fn(),
  cancelLeaveRequest: vi.fn(),
  createLeaveRequest: vi.fn().mockResolvedValue({}),
  previewLeaveRequest: vi.fn(),
  setLeaveAllowance: vi.fn()
}));

vi.mock("react-router-dom", async (original) => ({
  ...(await original<typeof import("react-router-dom")>()),
  useOutletContext: () => ({ refreshPendingLeaveCount: vi.fn() })
}));

describe("Leave for a few hours", () => {
  beforeEach(() => {
    vi.mocked(createLeaveRequest).mockClear();
    vi.mocked(previewLeaveRequest).mockReset().mockResolvedValue({
      workingDays: 0.27, hours: 2, deductsFromBalance: true, remainingBalanceHours: null,
      exceedsBalance: false, overlapsExistingRequest: false
    });
  });

  it("asks for a start and end time, then requests just those hours", async () => {
    render(
      <MemoryRouter>
        <ConfirmProvider>
          <LeavePage />
        </ConfirmProvider>
      </MemoryRouter>
    );

    fireEvent.change(await screen.findByLabelText("Part of the day"), { target: { value: "Hours" } });
    const submit = screen.getByRole("button", { name: "Send request" });
    expect(submit).toBeDisabled();

    fireEvent.change(screen.getByLabelText("Start time"), { target: { value: "14:00" } });
    fireEvent.change(screen.getByLabelText("End time"), { target: { value: "16:00" } });

    await waitFor(() =>
      expect(previewLeaveRequest).toHaveBeenLastCalledWith("pto", expect.any(String), expect.any(String), "Hours", "Hours", "14:00", "16:00")
    );
    expect(await screen.findByText("2h")).toBeInTheDocument();
    expect(screen.queryByText(/working day/)).not.toBeInTheDocument();

    await act(async () => fireEvent.click(submit));
    expect(createLeaveRequest).toHaveBeenCalledWith(
      expect.objectContaining({ firstDayPart: "Hours", lastDayPart: "Hours", startTime: "14:00", endTime: "16:00" })
    );
  });
});
