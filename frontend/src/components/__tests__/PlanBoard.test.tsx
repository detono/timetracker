import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import "../../i18n";
import { PlanBoard } from "../PlanBoard";
import { usePublicHolidays } from "../../utils/publicHolidays";
import type { TimeEntry, WorkSchedule } from "../../types";

vi.mock("../../api/companyApi", () => ({
  getPublicHolidays: vi.fn(async (year: number) =>
    year === 2026
      ? [
          { id: "1", date: "2026-11-11", name: "Wapenstilstand" },
          { id: "2", date: "2026-12-25", name: "Kerstmis" }
        ]
      : [{ id: "3", date: "2027-01-01", name: "Nieuwjaar" }]
  )
}));

const week = ["2026-11-09", "2026-11-10", "2026-11-11", "2026-11-12", "2026-11-13", "2026-11-14", "2026-11-15"];

const entry: TimeEntry = {
  id: "e1",
  userId: "u1",
  userFullName: "Jonathan Peeters",
  hourTypeId: "h1",
  projectId: null,
  localizedHourTypeNames: { en: "Work" },
  hourTypeColor: "#932e4a",
  workDate: "2026-11-09",
  startTime: "09:00:00",
  endTime: "17:00:00",
  breakMinutes: 30,
  durationHours: 7.5,
  notes: null,
  projectName: null,
  location: null,
  latitude: null,
  longitude: null
};

describe("PlanBoard holidays", () => {
  it("names the holiday in its day and shades that column", () => {
    const { container } = render(
      <PlanBoard days={week} entries={[entry]} holidays={{ "2026-11-11": "Wapenstilstand" }} />
    );
    expect(screen.getByText("Wapenstilstand")).toBeInTheDocument();
    expect(container.querySelectorAll(".planboard__cell--holiday")).toHaveLength(1);
  });

  it("still shows the week when it has a holiday but no hours yet", () => {
    render(<PlanBoard days={week} entries={[]} holidays={{ "2026-11-11": "Wapenstilstand" }} />);
    expect(screen.getByText("Wapenstilstand")).toBeInTheDocument();
    expect(screen.getByText("No hours logged for this period yet.")).toBeInTheDocument();
  });

  it("shows only the empty message for a week without hours or holidays", () => {
    const { container } = render(<PlanBoard days={week} entries={[]} />);
    expect(screen.getByText("No hours logged for this period yet.")).toBeInTheDocument();
    expect(container.querySelector(".planboard")).toBeNull();
  });
});

function HolidayList({ from, to }: { from: string; to: string }) {
  const holidays = usePublicHolidays(from, to);
  return <p>{Object.values(holidays).join(", ")}</p>;
}

describe("usePublicHolidays", () => {
  it("keeps only the days in view", async () => {
    render(<HolidayList from="2026-11-09" to="2026-11-15" />);
    await waitFor(() => expect(screen.getByText("Wapenstilstand")).toBeInTheDocument());
  });

  it("loads both years for a week around New Year", async () => {
    render(<HolidayList from="2026-12-28" to="2027-01-03" />);
    await waitFor(() => expect(screen.getByText("Nieuwjaar")).toBeInTheDocument());
  });
});

const schedule: WorkSchedule = {
  userId: "u1",
  source: "Company",
  weeklyHours: 37.5,
  days: (["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"] as const).map((dayOfWeek) => ({
    dayOfWeek,
    startTime: "09:00:00",
    endTime: "17:00:00",
    breakMinutes: 30
  }))
};

describe("PlanBoard rows", () => {
  it("shows initials and the week's hours against the schedule", () => {
    render(<PlanBoard days={week} entries={[entry]} schedules={{ u1: schedule }} holidays={{}} />);
    expect(screen.getByText("JP")).toBeInTheDocument();
    expect(screen.getByText("7h 30m / 37h 30m")).toBeInTheDocument();
  });

  it("shows only the worked hours for someone without a schedule", () => {
    const { container } = render(<PlanBoard days={week} entries={[entry]} />);
    expect(container.querySelector(".planboard__week")).toHaveTextContent(/^7h 30m$/);
    expect(container.querySelector(".planboard__progress")).toBeNull();
    expect(container.querySelector(".planboard__diff")).toBeNull();
  });

  it("shows how far a day is from the schedule", () => {
    const short = { ...entry, endTime: "15:30:00", durationHours: 6 };
    render(<PlanBoard days={week} entries={[short]} schedules={{ u1: schedule }} />);
    expect(screen.getByText("−1h 30m")).toBeInTheDocument();
  });

  it("hatches the part of an entry outside the schedule", () => {
    const late = { ...entry, startTime: "09:00:00", endTime: "19:00:00", durationHours: 9.5 };
    const { container } = render(<PlanBoard days={week} entries={[late]} schedules={{ u1: schedule }} />);
    expect(container.querySelectorAll(".planboard__seg")).toHaveLength(2);
    expect(container.querySelectorAll(".planboard__seg--outside")).toHaveLength(1);
  });

  it("draws a run of leave days as one bar", () => {
    const leave = ["2026-11-09", "2026-11-10", "2026-11-11"].map((workDate, i) => ({
      ...entry,
      id: `l${i}`,
      workDate,
      hourTypeId: "leave",
      localizedHourTypeNames: { en: "Holiday leave" }
    }));
    const { container } = render(
      <PlanBoard days={week} entries={leave} leaveHourTypeIds={new Set(["leave"])} />
    );
    expect(container.querySelectorAll(".planboard__span")).toHaveLength(1);
    expect(screen.getByText(/Holiday leave · Mon 9 Nov – Wed 11 Nov/)).toBeInTheDocument();
  });
});

describe("PlanBoard editing", () => {
  it("opens the only entry of a day straight away", () => {
    const onEdit = vi.fn();
    render(<PlanBoard days={week} entries={[entry]} onEdit={onEdit} />);
    fireEvent.click(screen.getByRole("button", { name: /Jonathan Peeters, Mon 9 Nov/ }));
    expect(onEdit).toHaveBeenCalledWith(entry);
  });

  it("lists a day's entries first when there are several, then opens the one picked", () => {
    const onEdit = vi.fn();
    const support = { ...entry, id: "e2", startTime: "13:00:00", endTime: "17:00:00", localizedHourTypeNames: { en: "Support" } };
    const dev = { ...entry, id: "e1", startTime: "09:00:00", endTime: "12:00:00", localizedHourTypeNames: { en: "Development" } };
    render(<PlanBoard days={week} entries={[support, dev]} onEdit={onEdit} />);

    fireEvent.click(screen.getByRole("button", { name: /Jonathan Peeters, Mon 9 Nov/ }));
    expect(onEdit).not.toHaveBeenCalled();
    const list = screen.getByRole("dialog");
    expect(list).toHaveTextContent(/Development.*09:00–12:00.*Support.*13:00–17:00/);

    fireEvent.click(screen.getByRole("button", { name: /Support/ }));
    expect(onEdit).toHaveBeenCalledWith(support);
    expect(screen.queryByRole("dialog")).toBeNull();
  });

  it("closes the list with Escape", () => {
    const second = { ...entry, id: "e2", startTime: "13:00:00" };
    render(<PlanBoard days={week} entries={[entry, second]} onEdit={vi.fn()} />);
    fireEvent.click(screen.getByRole("button", { name: /Jonathan Peeters, Mon 9 Nov/ }));
    fireEvent.keyDown(document, { key: "Escape" });
    expect(screen.queryByRole("dialog")).toBeNull();
  });

  it("shows a locked entry without letting it be edited", () => {
    const onEdit = vi.fn();
    render(<PlanBoard days={week} entries={[entry]} onEdit={onEdit} isLocked={() => true} />);
    fireEvent.click(screen.getByRole("button", { name: /Jonathan Peeters, Mon 9 Nov/ }));
    expect(onEdit).not.toHaveBeenCalled();
    expect(screen.getByRole("dialog")).toHaveTextContent("09:00–17:00");
  });
});
