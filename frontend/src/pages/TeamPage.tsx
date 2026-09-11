import { useCallback, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { getTeamTimeEntries } from "../api/timeEntriesApi";
import { extractErrorMessage } from "../api/client";
import { ViewToggle, type ViewMode } from "../components/ViewToggle";
import { ListView } from "../components/ListView";
import { PlanBoard } from "../components/PlanBoard";
import { WeekNavigator } from "../components/WeekNavigator";
import { getWeekRange } from "../utils/dateRange";
import type { TimeEntry } from "../types";

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

  const { from, to, days } = getWeekRange(anchorDate);

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

      <WeekNavigator anchorDate={anchorDate} onChange={setAnchorDate} from={from} to={to} />

      {error && <p className="form__error">{error}</p>}

      {loading ? (
        <p className="empty-state">{t('common.loading')}</p>
      ) : view === "list" ? (
        <ListView entries={entries} showEmployeeColumn />
      ) : (
        <PlanBoard days={days} entries={entries} />
      )}
    </div>
  );
}