import { FormEvent, useState } from "react";
import { useTranslation } from "react-i18next";
import type { HourType, TimeEntry } from "../types";

export interface TimeEntryFormValues {
  hourTypeId: string;
  workDate: string;
  startTime: string;
  endTime: string;
  breakMinutes: number;
  notes: string;
}

interface Props {
  initial?: TimeEntry;
  hourTypes: HourType[];
  onSubmit: (values: TimeEntryFormValues) => Promise<void>;
  onCancel?: () => void;
}

export function TimeEntryForm({ initial, hourTypes, onSubmit, onCancel }: Props) {
  const { t } = useTranslation();

  const [values, setValues] = useState<TimeEntryFormValues>({
    hourTypeId: initial?.hourTypeId ?? hourTypes[0]?.id ?? "",
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
      setError(err instanceof Error ? err.message : t('timeEntryForm.errorSave'));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <form className="form form--entry" onSubmit={handleSubmit}>
      <div className="form__row">
        <label className="form__field">
          <span>{t('timeEntryForm.type')}</span>
          <select
            value={values.hourTypeId}
            onChange={(e) => setValues({ ...values, hourTypeId: e.target.value })}
            required
          >
            {hourTypes.length === 0 && <option value="">{t('timeEntryForm.noHourTypes')}</option>}
            {hourTypes.map((t) => (
              <option key={t.id} value={t.id}>
                {t.name}
              </option>
            ))}
          </select>
        </label>
        <label className="form__field">
          <span>{t('timeEntryForm.date')}</span>
          <input
            type="date"
            value={values.workDate}
            onChange={(e) => setValues({ ...values, workDate: e.target.value })}
            required
          />
        </label>
        <label className="form__field">
          <span>{t('timeEntryForm.start')}</span>
          <input
            type="time"
            value={values.startTime}
            onChange={(e) => setValues({ ...values, startTime: e.target.value })}
            required
          />
        </label>
        <label className="form__field">
          <span>{t('timeEntryForm.end')}</span>
          <input
            type="time"
            value={values.endTime}
            onChange={(e) => setValues({ ...values, endTime: e.target.value })}
            required
          />
        </label>
        <label className="form__field form__field--narrow">
          <span>{t('timeEntryForm.break')}</span>
          <input
            type="number"
            min={0}
            value={values.breakMinutes}
            onChange={(e) => setValues({ ...values, breakMinutes: Number(e.target.value) })}
          />
        </label>
      </div>

      <label className="form__field">
        <span>{t('timeEntryForm.notes')}</span>
        <input
          type="text"
          value={values.notes}
          onChange={(e) => setValues({ ...values, notes: e.target.value })}
          placeholder={t('timeEntryForm.notesPlaceholder')}
        />
      </label>

      {error && <p className="form__error">{error}</p>}

      <div className="form__actions">
        <button type="submit" className="btn btn--primary" disabled={submitting || !values.hourTypeId}>
          {submitting ? t('common.saving') : initial ? t('timeEntryForm.saveChanges') : t('timeEntryForm.logHoursBtn')}
        </button>
        {onCancel && (
          <button type="button" className="btn btn--ghost" onClick={onCancel}>
            {t('common.cancel')}
          </button>
        )}
      </div>
    </form>
  );
}