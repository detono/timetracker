import { describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import "../../i18n";
import { PlanBoard } from "../PlanBoard";
import { usePublicHolidays } from "../../utils/publicHolidays";
import type { TimeEntry } from "../../types";

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
