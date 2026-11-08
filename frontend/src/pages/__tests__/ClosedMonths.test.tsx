import { beforeEach, describe, expect, it, vi } from "vitest";
import { act, fireEvent, render, screen } from "@testing-library/react";
import "../../i18n";
import { ReportsPage } from "../ReportsPage";
import { ConfirmProvider } from "../../components/ConfirmDialog";
import { lockMonth } from "../../api/lockedMonthsApi";

vi.mock("../../auth/AuthContext", () => ({
  useAuth: () => ({ user: { role: "Employer", fullName: "Erik Employer" } })
}));

vi.mock("../../api/lockedMonthsApi", () => ({
  getLockedMonths: vi.fn().mockResolvedValue([]),
  lockMonth: vi.fn().mockResolvedValue({}),
  unlockMonth: vi.fn().mockResolvedValue(undefined)
}));

describe("Closed months", () => {
  beforeEach(() => vi.mocked(lockMonth).mockClear());

  it("asks in a dialog before locking a month", async () => {
    render(
      <ConfirmProvider>
        <ReportsPage />
      </ConfirmProvider>
    );
    const [lastMonth] = (await screen.findAllByRole("button", { name: "Lock" })).slice(1);

    fireEvent.click(lastMonth);
    const dialog = screen.getByRole("dialog", { name: /^Lock .+\?$/ });
    expect(dialog).toHaveAccessibleDescription(/Nobody can add, change or delete hours/);

    await act(async () => fireEvent.click(screen.getByRole("button", { name: "Cancel" })));
    expect(lockMonth).not.toHaveBeenCalled();

    fireEvent.click(lastMonth);
    const confirmButton = screen.getAllByRole("button", { name: "Lock" }).find((b) => dialog.contains(b));
    await act(async () => fireEvent.click(confirmButton!));
    expect(lockMonth).toHaveBeenCalledOnce();
  });
});
