import { useCallback, useEffect, useState } from "react";
import { useAuth } from "../auth/AuthContext";
import {
  createTimeEntry,
  deleteTimeEntry,
  getMyTimeEntries,
  updateTimeEntry
} from "../api/timeEntriesApi";
import { extractErrorMessage } from "../api/client";
import { ViewToggle, type ViewMode } from "../components/ViewToggle";
import { ListView } from "../components/ListView";
import { PlanBoard } from "../components/PlanBoard";
import { TimeEntryForm, type TimeEntryFormValues } from "../components/TimeEntryForm";
import { WeekNavigator } from "../components/WeekNavigator";
import { getWeekRange, hoursToHm } from "../utils/dateRange";
import type { TimeEntry } from "../types";

export function DashboardPage() {
  const { user } = useAuth();
  const [anchorDate, setAnchorDate] = useState(new Date());
  const [entries, setEntries] = useState<TimeEntry[]>([]);
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
      const data = await getMyTimeEntries(user?.userId, from, to);
      setEntries(data);
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
    if (!confirm("Delete this time entry?")) return;
    await deleteTimeEntry(entry.id);
    await loadEntries();
  }

  const totalHours = entries.reduce((sum, e) => sum + e.durationHours, 0);

  return (
    <div className="page">
      <div className="page__header">
        <div>
          <h1>My hours</h1>
          <p className="page__subtitle">
            Week total: <strong>{hoursToHm(totalHours)}</strong>
          </p>
        </div>
        <div className="page__header-actions">
          <ViewToggle value={view} onChange={setView} />
          <button className="btn btn--primary" onClick={() => setShowForm(true)}>
            + Log hours
          </button>
        </div>
      </div>

      <WeekNavigator anchorDate={anchorDate} onChange={setAnchorDate} from={from} to={to} />

      {(showForm || editing) && (
        <div className="panel">
          <h2>{editing ? "Edit entry" : "Log new hours"}</h2>
          <TimeEntryForm
            initial={editing ?? undefined}
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
        <p className="empty-state">Loading…</p>
      ) : view === "list" ? (
        <ListView entries={entries} onEdit={setEditing} onDelete={handleDelete} />
      ) : (
        <PlanBoard days={days} entries={entries} onEdit={setEditing} />
      )}
    </div>
  );
}
