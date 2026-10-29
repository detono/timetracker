import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import "../../i18n";
import { TimeEntryForm } from "../TimeEntryForm";
import type { HourType } from "../../types";

const work = {
  id: "h1", localizedNames: { en: "Work" }, colorHex: "#932e4a", isActive: true, isDefault: true,
  isLeave: false, deductsFromLeaveBalance: false
} as HourType;

function renderForm(lockedMonths?: Set<string>) {
  const onSubmit = vi.fn().mockResolvedValue(undefined);
  render(<TimeEntryForm hourTypes={[work]} projects={[]} onSubmit={onSubmit} lockedMonths={lockedMonths} />);
  return onSubmit;
}

const dateField = () => document.querySelector<HTMLInputElement>('input[type="date"]')!;
const submit = () => screen.getByRole("button", { name: "Log hours" });

describe("TimeEntryForm", () => {
  it("logs hours with the default hour type", async () => {
    const onSubmit = renderForm();
    fireEvent.change(dateField(), { target: { value: "2026-10-05" } });
    fireEvent.click(submit());

    await waitFor(() => expect(onSubmit).toHaveBeenCalled());
    expect(onSubmit.mock.calls[0][0]).toMatchObject({
      hourTypeId: "h1", workDate: "2026-10-05", startTime: "09:00", endTime: "17:00"
    });
  });

  it("won't log hours in a locked month and says why", () => {
    const onSubmit = renderForm(new Set(["2026-09"]));
    fireEvent.change(dateField(), { target: { value: "2026-09-15" } });

    expect(screen.getByText(/This month is locked/)).toBeInTheDocument();
    expect(submit()).toBeDisabled();
    fireEvent.click(submit());
    expect(onSubmit).not.toHaveBeenCalled();

    fireEvent.change(dateField(), { target: { value: "2026-10-01" } });
    expect(screen.queryByText(/This month is locked/)).toBeNull();
    expect(submit()).toBeEnabled();
  });

  it("shows the server's message when saving fails", async () => {
    const onSubmit = vi.fn().mockRejectedValue(new Error("boom"));
    render(<TimeEntryForm hourTypes={[work]} projects={[]} onSubmit={onSubmit} />);
    fireEvent.click(submit());
    expect(await screen.findByText("Something went wrong. Please try again.")).toBeInTheDocument();
  });
});
