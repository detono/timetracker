import { FormEvent, useState } from "react";
import type { TimeEntry } from "../types";

export interface TimeEntryFormValues {
  workDate: string;
  startTime: string;
  endTime: string;
  breakMinutes: number;
  notes: string;
}

interface Props {
  initial?: TimeEntry;
  onSubmit: (values: TimeEntryFormValues) => Promise<void>;
  onCancel?: () => void;
}

export function TimeEntryForm({ initial, onSubmit, onCancel }: Props) {
  const [values, setValues] = useState<TimeEntryFormValues>({
    workDate: initial?.workDate ?? new Date().toISOString().slice(0, 10),
    startTime: initial?.startTime.slice(0, 5) ?? "09:00",
    endTime: initial?.endTime.slice(0, 5) ?? "17:00",
    breakMinutes: initial?.breakMinutes ?? 30,
    notes: initial?.notes ?? ""
  });
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await onSubmit(values);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not save this entry.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <form className="form form--entry" onSubmit={handleSubmit}>
      <div className="form__row">
        <label className="form__field">
          <span>Date</span>
          <input
            type="date"
            value={values.workDate}
            onChange={(e) => setValues({ ...values, workDate: e.target.value })}
            required
          />
        </label>
        <label className="form__field">
          <span>Start</span>
          <input
            type="time"
            value={values.startTime}
            onChange={(e) => setValues({ ...values, startTime: e.target.value })}
            required
          />
        </label>
        <label className="form__field">
          <span>End</span>
          <input
            type="time"
            value={values.endTime}
            onChange={(e) => setValues({ ...values, endTime: e.target.value })}
            required
          />
        </label>
        <label className="form__field form__field--narrow">
          <span>Break (min)</span>
          <input
            type="number"
            min={0}
            value={values.breakMinutes}
            onChange={(e) => setValues({ ...values, breakMinutes: Number(e.target.value) })}
          />
        </label>
      </div>

      <label className="form__field">
        <span>Notes (optional)</span>
        <input
          type="text"
          value={values.notes}
          onChange={(e) => setValues({ ...values, notes: e.target.value })}
          placeholder="What did you work on?"
        />
      </label>

      {error && <p className="form__error">{error}</p>}

      <div className="form__actions">
        <button type="submit" className="btn btn--primary" disabled={submitting}>
          {submitting ? "Saving…" : initial ? "Save changes" : "Log hours"}
        </button>
        {onCancel && (
          <button type="button" className="btn btn--ghost" onClick={onCancel}>
            Cancel
          </button>
        )}
      </div>
    </form>
  );
}
