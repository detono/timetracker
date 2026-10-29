import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import "../../i18n";
import { TimeEntryForm } from "../TimeEntryForm";
import type { HourType, WorkSchedule } from "../../types";
import { scheduledTimes } from "../../utils/workSchedule";

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

  describe("with a work schedule", () => {
    // Thursday 08:30-17:30 with 45 minutes break, Friday 08:30-15:30; nothing at the weekend.
    const schedule: WorkSchedule = {
      userId: "u1",
      source: "Company",
      weeklyHours: 14.75,
      days: [
        { dayOfWeek: "Thursday", startTime: "08:30:00", endTime: "17:30:00", breakMinutes: 45 },
        { dayOfWeek: "Friday", startTime: "08:30:00", endTime: "15:30:00", breakMinutes: 30 }
      ]
    };
    const field = (type: string, index = 0) => document.querySelectorAll<HTMLInputElement>(`input[type="${type}"]`)[index];

    function renderWithSchedule() {
      const onSubmit = vi.fn().mockResolvedValue(undefined);
      render(<TimeEntryForm hourTypes={[work]} projects={[]} onSubmit={onSubmit} schedule={schedule} />);
      return onSubmit;
    }

    it("proposes the scheduled hours of the chosen day", () => {
      renderWithSchedule();
      fireEvent.change(dateField(), { target: { value: "2026-10-29" } });
      expect(field("time", 0).value).toBe("08:30");
      expect(field("time", 1).value).toBe("17:30");
      expect(field("number").value).toBe("45");

      fireEvent.change(dateField(), { target: { value: "2026-10-30" } });
      expect(field("time", 1).value).toBe("15:30");
      expect(field("number").value).toBe("30");
    });

    it("keeps times you changed yourself when the date changes", () => {
      renderWithSchedule();
      fireEvent.change(dateField(), { target: { value: "2026-10-29" } });
      fireEvent.change(field("time", 1), { target: { value: "19:00" } });
      fireEvent.change(dateField(), { target: { value: "2026-10-30" } });
      expect(field("time", 1).value).toBe("19:00");
    });

    it("keeps an existing entry's own times", () => {
      const entry = {
        id: "e1", hourTypeId: "h1", projectId: null, workDate: "2026-10-29", startTime: "10:00:00",
        endTime: "12:00:00", breakMinutes: 0, notes: null, location: null, latitude: null, longitude: null
      } as unknown as Parameters<typeof TimeEntryForm>[0]["initial"];
      render(<TimeEntryForm hourTypes={[work]} projects={[]} onSubmit={vi.fn()} schedule={schedule} initial={entry} />);
      fireEvent.change(dateField(), { target: { value: "2026-10-30" } });
      expect(field("time", 0).value).toBe("10:00");
      expect(field("time", 1).value).toBe("12:00");
    });
  });
});

describe("scheduledTimes", () => {
  const schedule = {
    userId: null, source: "Company", weeklyHours: 8,
    days: [{ dayOfWeek: "Monday", startTime: "08:00:00", endTime: "16:00:00", breakMinutes: 30 }]
  } as WorkSchedule;

  it("gives the day's hours, or nothing on a day off or without a schedule", () => {
    expect(scheduledTimes(schedule, "2026-10-26")).toEqual({ startTime: "08:00", endTime: "16:00", breakMinutes: 30 });
    expect(scheduledTimes(schedule, "2026-10-31")).toBeNull();
    expect(scheduledTimes(null, "2026-10-26")).toBeNull();
    expect(scheduledTimes(schedule, "")).toBeNull();
  });
});
