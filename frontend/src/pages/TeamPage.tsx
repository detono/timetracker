import { useCallback, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { getTeamTimeEntries } from "../api/timeEntriesApi";
import { getTeamHoursOverview } from "../api/hoursOverviewApi";
import { getWorkSchedule } from "../api/leaveApi";
import { getHourTypes } from "../api/hourTypesApi";
import { extractErrorMessage } from "../api/client";
import { ViewToggle, type ViewMode } from "../components/ViewToggle";
import { ListView } from "../components/ListView";
import { PlanBoard } from "../components/PlanBoard";
import { WeekNavigator } from "../components/WeekNavigator";
import { TeamHoursTable } from "../components/TeamHoursTable";
import { getWeekRange } from "../utils/dateRange";
import { usePublicHolidays } from "../utils/publicHolidays";
import { localToday } from "../utils/hoursOverview";
import type { TimeEntry, WorkSchedule } from "../types";

/**
 * Shows every employee the caller has authority over: everyone, for an Employer,
 * or the caller plus their supervisees, for an Employee with delegated authority.
 */
export function TeamPage() {
  const { t } = useTranslation();
  const [anchorDate, setAnchorDate] = useState(new Date());
  const [entries, setEntries] = useState<TimeEntry[]>([]);
  const [view, setView] = useState<ViewMode>("planboard");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Schedules of the people whose hours are compared with one, and which hour types are leave:
  // the planboard uses them to show scheduled hours and to draw leave as one bar.
  const [schedules, setSchedules] = useState<Record<string, WorkSchedule>>({});
  const [leaveHourTypeIds, setLeaveHourTypeIds] = useState<ReadonlySet<string>>(new Set());
  useEffect(() => {
    let cancelled = false;
    getTeamHoursOverview(localToday())
      .then((team) => Promise.all(team.map((m) => getWorkSchedule(m.userId))))
      .then((list) => !cancelled && setSchedules(Object.fromEntries(list.map((s) => [s.userId ?? "", s]))))
      .catch(() => !cancelled && setSchedules({}));
    getHourTypes(true)
      .then((types) => !cancelled && setLeaveHourTypeIds(new Set(types.filter((h) => h.isLeave).map((h) => h.id))))
      .catch(() => !cancelled && setLeaveHourTypeIds(new Set()));
    return () => {
      cancelled = true;
    };
  }, []);

  const { from, to, days } = getWeekRange(anchorDate);
  const holidays = usePublicHolidays(from, to);

  const loadEntries = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getTeamTimeEntries(from, to);
      setEntries(data);
    } catch (err) {
      setError(extractErrorMessage(err)); // The API error translation can be handled in a wrapper later if needed
    } finally {
      setLoading(false);
    }
  }, [from, to]);

  useEffect(() => {
    loadEntries();
  }, [loadEntries]);

  return (
    <div className="page">
      <div className="page__header">
        <div>
          <h1>{t('team.title')}</h1>
          <p className="page__subtitle">{t('team.subtitle')}</p>
        </div>
        <ViewToggle value={view} onChange={setView} />
      </div>

      <TeamHoursTable />

      <WeekNavigator anchorDate={anchorDate} onChange={setAnchorDate} from={from} to={to} />

      {error && <p className="form__error">{error}</p>}

      {loading ? (
        <p className="empty-state">{t('common.loading')}</p>
      ) : view === "list" ? (
        <ListView entries={entries} showEmployeeColumn />
      ) : (
        <PlanBoard
          days={days}
          entries={entries}
          holidays={holidays}
          schedules={schedules}
          leaveHourTypeIds={leaveHourTypeIds}
        />
      )}
    </div>
  );
}