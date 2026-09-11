import { useCallback, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { useAuth } from "../auth/AuthContext";
import {
  createTimeEntry,
  deleteTimeEntry,
  getMyTimeEntries,
  updateTimeEntry
} from "../api/timeEntriesApi";
import { getHourTypes } from "../api/hourTypesApi";
import { extractErrorMessage } from "../api/client";
import { ViewToggle, type ViewMode } from "../components/ViewToggle";
import { ListView } from "../components/ListView";
import { PlanBoard } from "../components/PlanBoard";
import { TimeEntryForm, type TimeEntryFormValues } from "../components/TimeEntryForm";
import { WeekNavigator } from "../components/WeekNavigator";
import { getWeekRange, hoursToHm } from "../utils/dateRange";
import type { HourType, TimeEntry } from "../types";

export function DashboardPage() {
  const { t } = useTranslation();
  const { user } = useAuth();
  const [anchorDate, setAnchorDate] = useState(new Date());
  const [entries, setEntries] = useState<TimeEntry[]>([]);
  const [hourTypes, setHourTypes] = useState<HourType[]>([]);
  const [view, setView] = useState<ViewMode>("list");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);
  const [editing, setEditing] = useState<TimeEntry | null>(null);

  const { from, to, days } = getWeekRange(anchorDate);

  const loadEntries = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [entriesData, hourTypesData] = await Promise.all([
        getMyTimeEntries(user?.userId, from, to),
        getHourTypes()
      ]);
      setEntries(entriesData);
      setHourTypes(hourTypesData);
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }, [user?.userId, from, to]);

  useEffect(() => {
    loadEntries();
  }, [loadEntries]);

  async function handleCreate(values: TimeEntryFormValues) {
    await createTimeEntry({
      hourTypeId: values.hourTypeId,
      workDate: values.workDate,
      startTime: values.startTime,
      endTime: values.endTime,
      breakMinutes: values.breakMinutes,
      notes: values.notes || null
    });
    setShowForm(false);
    await loadEntries();
  }

  async function handleUpdate(values: TimeEntryFormValues) {
    if (!editing) return;
    await updateTimeEntry(editing.id, {
      hourTypeId: values.hourTypeId,
      workDate: values.workDate,
      startTime: values.startTime,
      endTime: values.endTime,
      breakMinutes: values.breakMinutes,
      notes: values.notes || null
    });
    setEditing(null);
    await loadEntries();
  }

  async function handleDelete(entry: TimeEntry) {
    if (!confirm(t('dashboard.confirmDelete'))) return;
    await deleteTimeEntry(entry.id);
    await loadEntries();
  }

  const totalHours = entries.reduce((sum, e) => sum + e.durationHours, 0);

  return (
    <div className="page">
      <div className="page__header">
        <div>
          <h1>{t('dashboard.title')}</h1>
          <p className="page__subtitle">
            {t('dashboard.weekTotal')} <strong>{hoursToHm(totalHours)}</strong>
          </p>
        </div>
        <div className="page__header-actions">
          <ViewToggle value={view} onChange={setView} />
          <button className="btn btn--primary" onClick={() => setShowForm(true)}>
            {t('dashboard.logHoursBtn')}
          </button>
        </div>
      </div>

      <WeekNavigator anchorDate={anchorDate} onChange={setAnchorDate} from={from} to={to} />

      {(showForm || editing) && (
        <div className="panel">
          <h2>{editing ? t('dashboard.editEntry') : t('dashboard.logNewHours')}</h2>
          <TimeEntryForm
            initial={editing ?? undefined}
            hourTypes={hourTypes}
            onSubmit={editing ? handleUpdate : handleCreate}
            onCancel={() => {
              setShowForm(false);
              setEditing(null);
            }}
          />
        </div>
      )}

      {error && <p className="form__error">{error}</p>}

      {loading ? (
        <p className="empty-state">{t('common.loading')}</p>
      ) : view === "list" ? (
        <ListView entries={entries} onEdit={setEditing} onDelete={handleDelete} />
      ) : (
        <PlanBoard days={days} entries={entries} onEdit={setEditing} />
      )}
    </div>
  );
}