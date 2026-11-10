import { beforeEach, describe, expect, it, vi } from "vitest";
import { act, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import "../../i18n";
import { LeavePage } from "../LeavePage";
import { ConfirmProvider } from "../../components/ConfirmDialog";
import { approveLeaveRequest, getLeaveRequests, rejectLeaveRequest } from "../../api/leaveApi";
import type { LeaveRequest } from "../../types";

vi.mock("../../auth/AuthContext", () => ({
  useAuth: () => ({ user: { userId: "boss", role: "Employer", fullName: "Erik Employer" } })
}));

vi.mock("../../api/hourTypesApi", () => ({ getHourTypes: vi.fn().mockResolvedValue([]) }));

vi.mock("../../api/leaveApi", () => ({
  getLeaveRequests: vi.fn(),
  getLeaveBalances: vi.fn().mockResolvedValue([]),
  approveLeaveRequest: vi.fn().mockResolvedValue({}),
  rejectLeaveRequest: vi.fn().mockResolvedValue({}),
  cancelLeaveRequest: vi.fn(),
  createLeaveRequest: vi.fn(),
  previewLeaveRequest: vi.fn(),
  setLeaveAllowance: vi.fn()
}));

vi.mock("react-router-dom", async (original) => ({
  ...(await original<typeof import("react-router-dom")>()),
  useOutletContext: () => ({ refreshPendingLeaveCount: vi.fn() })
}));

const pending: LeaveRequest = {
  id: "req-1", userId: "eva", userFullName: "Eva Peeters", hourTypeId: "t", localizedHourTypeNames: { en: "Paid time off" },
  hourTypeColor: "#2a7ab0", startDate: "2026-11-04", endDate: "2026-11-04", firstDayPart: "Full", lastDayPart: "Full",
  hours: 7.5, comment: null, status: "Pending", reviewedByName: null, reviewedAtUtc: null, reviewComment: null,
  createdAtUtc: "2026-11-01T08:00:00Z", canReview: true, canCancel: false, remainingBalanceHours: null,
  exceedsBalance: false, inLockedMonth: false
};

function Search() {
  return <span data-testid="search">{useLocation().search}</span>;
}

function renderAt(url: string) {
  render(
    <MemoryRouter initialEntries={[url]}>
      <ConfirmProvider>
        <Routes>
          <Route path="/leave" element={<><LeavePage /><Search /></>} />
        </Routes>
      </ConfirmProvider>
    </MemoryRouter>
  );
}

describe("Leave email links", () => {
  beforeEach(() => {
    vi.mocked(approveLeaveRequest).mockClear();
    vi.mocked(rejectLeaveRequest).mockClear();
    vi.mocked(getLeaveRequests).mockImplementation(async (scope) => (scope === "ToReview" ? [pending] : []));
  });

  it("asks to confirm the approval, then approves", async () => {
    renderAt("/leave?review=req-1&decision=approve");

    const dialog = await screen.findByRole("dialog", { name: "Approve the request of Eva Peeters?" });
    // The link is used once: it disappears from the address.
    await waitFor(() => expect(screen.getByTestId("search")).toHaveTextContent(/^$/));
    expect(approveLeaveRequest).not.toHaveBeenCalled();

    const confirm = screen.getAllByRole("button", { name: "Approve" }).find((b) => dialog.contains(b));
    await act(async () => fireEvent.click(confirm!));
    expect(approveLeaveRequest).toHaveBeenCalledWith("req-1", null);
  });

  it("opens the reject dialog", async () => {
    renderAt("/leave?review=req-1&decision=reject");

    const dialog = await screen.findByRole("dialog", { name: "Reject the request of Eva Peeters?" });
    const confirm = screen.getAllByRole("button", { name: "Reject" }).find((b) => dialog.contains(b));
    await act(async () => fireEvent.click(confirm!));
    expect(rejectLeaveRequest).toHaveBeenCalledWith("req-1", null);
  });

  it("says so when the request was already handled", async () => {
    renderAt("/leave?review=gone&decision=approve");

    expect(await screen.findByRole("status")).toHaveTextContent("already handled");
    expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
  });
});
