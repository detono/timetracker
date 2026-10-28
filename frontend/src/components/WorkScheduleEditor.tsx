import { FormEvent, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { getWorkSchedule, updateWorkSchedule } from "../api/leaveApi";
import { extractErrorMessage } from "../api/client";
import { hoursToHm } from "../utils/dateRange";
import { scheduleDayHours, toHm, WEEKDAYS } from "../utils/leave";
import type { Weekday, WorkScheduleDay } from "../types";

interface DayRow {
  dayOfWeek: Weekday;
  works: boolean;
  startTime: string;
  endTime: string;
  breakMinutes: number;
}

interface Props {
  userId: string;
  userName: string;
  onClose: () => void;
}

/** Lets an employer set someone's working hours per weekday. Unchecked days are days off. */
export function WorkScheduleEditor({ userId, userName, onClose }: Props) {
  const { t } = useTranslation();
  const [rows, setRows] = useState<DayRow[] | null>(null);
  const [isDefault, setIsDefault] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    let cancelled = false;
    setRows(null);
    setError(null);
    setSaved(false);
    getWorkSchedule(userId)
      .then((schedule) => {
        if (cancelled) return;
        setIsDefault(schedule.isDefault);
        setRows(
          WEEKDAYS.map((day) => {
            const saved = schedule.days.find((d) => d.dayOfWeek === day);
            return saved
              ? { dayOfWeek: day, works: true, startTime: toHm(saved.startTime), endTime: toHm(saved.endTime), breakMinutes: saved.breakMinutes }
              : { dayOfWeek: day, works: false, startTime: "09:00", endTime: "17:00", breakMinutes: 30 };
          })
        );
      })
      .catch((err) => !cancelled && setError(extractErrorMessage(err)));
    return () => {
      cancelled = true;
    };
  }, [userId]);

  function update(index: number, change: Partial<DayRow>) {
    setSaved(false);
    setRows((current) => current && current.map((row, i) => (i === index ? { ...row, ...change } : row)));
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (!rows) return;
    setSaving(true);
    setError(null);
    try {
      const days: WorkScheduleDay[] = rows
        .filter((r) => r.works)
        .map(({ dayOfWeek, startTime, endTime, breakMinutes }) => ({ dayOfWeek, startTime, endTime, breakMinutes }));
      await updateWorkSchedule(userId, days);
      setIsDefault(false);
      setSaved(true);
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  const weeklyHours = rows?.filter((r) => r.works).reduce((sum, r) => sum + scheduleDayHours(r), 0) ?? 0;

  return (
    <div className="panel">
      <h2>{t('workSchedule.titleFor', { name: userName })}</h2>
      <p className="form__hint">{t('workSchedule.hint')}</p>
      {isDefault && <p className="form__hint">{t('workSchedule.usingDefault')}</p>}

      {!rows ? (
        error ? <p className="form__error">{error}</p> : <p className="empty-state">{t('common.loading')}</p>
      ) : (
        <form className="form" onSubmit={handleSubmit}>
          <div className="schedule-grid">
            <span />
            <strong>{t('workSchedule.start')}</strong>
            <strong>{t('workSchedule.end')}</strong>
            <strong>{t('workSchedule.breakMinutes')}</strong>
            <span />
            {rows.map((row, i) => (
              <ScheduleRow key={row.dayOfWeek} row={row} onChange={(change) => update(i, change)} />
            ))}
          </div>

          <p className="form__hint">
            {t('workSchedule.weeklyTotal')} <strong>{hoursToHm(weeklyHours)}</strong>
          </p>
          {error && <p className="form__error">{error}</p>}
          {saved && <p className="form__success">{t('workSchedule.saved')}</p>}

          <div className="form__actions">
            <button className="btn btn--primary" type="submit" disabled={saving}>
              {saving ? t('common.saving') : t('common.save')}
            </button>
            <button type="button" className="btn btn--ghost" onClick={onClose}>
              {t('workSchedule.close')}
            </button>
          </div>
        </form>
      )}
    </div>
  );
}

function ScheduleRow({ row, onChange }: { row: DayRow; onChange: (change: Partial<DayRow>) => void }) {
  const { t } = useTranslation();
  const dayLabel = t(`weekdays.${row.dayOfWeek}`);

  return (
    <>
      <label className="schedule-grid__day">
        <input type="checkbox" checked={row.works} onChange={(e) => onChange({ works: e.target.checked })} />
        {dayLabel}
      </label>
      {row.works ? (
        <>
          <input
            type="time"
            aria-label={`${dayLabel} ${t('workSchedule.start')}`}
            value={row.startTime}
            onChange={(e) => onChange({ startTime: e.target.value })}
            required
          />
          <input
            type="time"
            aria-label={`${dayLabel} ${t('workSchedule.end')}`}
            value={row.endTime}
            onChange={(e) => onChange({ endTime: e.target.value })}
            required
          />
          <input
            type="number"
            min={0}
            step={5}
            aria-label={`${dayLabel} ${t('workSchedule.breakMinutes')}`}
            value={row.breakMinutes}
            onChange={(e) => onChange({ breakMinutes: Number(e.target.value) })}
            required
          />
          <span className="list-view__notes">{hoursToHm(scheduleDayHours(row))}</span>
        </>
      ) : (
        <>
          <span className="list-view__notes">{t('workSchedule.dayOff')}</span>
          <span />
          <span />
          <span />
        </>
      )}
    </>
  );
}
