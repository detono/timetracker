import { FormEvent, useEffect, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import type { HourType, TimeEntry, Project, WorkSchedule } from "../types";
import { localToday } from "../utils/hoursOverview";
import { scheduledTimes } from "../utils/workSchedule";
import { extractErrorMessage } from "../api/client";
import { isInLockedMonth } from "../utils/lockedMonths";
import { formatCoordinates, GeolocationError, getCurrentPosition, reverseGeocode } from "../utils/geolocation";

export interface TimeEntryFormValues {
  hourTypeId: string;
  projectId: string; // Added this
  workDate: string;
  startTime: string;
  endTime: string;
  breakMinutes: number;
  notes: string;
  location: string;
  latitude: number | null;
  longitude: number | null;
}

interface Props {
  initial?: TimeEntry;
  hourTypes: HourType[];
  projects: Project[]; // Added this
  onSubmit: (values: TimeEntryFormValues) => Promise<void>;
  onCancel?: () => void;
  /** Month keys ("2026-09") that are locked: hours can't be logged there. */
  lockedMonths?: Set<string>;
  /** Your work schedule: new entries start with that day's hours instead of 9 to 5. */
  schedule?: WorkSchedule | null;
}

const FALLBACK_TIMES = { startTime: "09:00", endTime: "17:00", breakMinutes: 30 };

export function TimeEntryForm({ initial, hourTypes, projects, onSubmit, onCancel, lockedMonths, schedule }: Props) {
  const { t, i18n } = useTranslation();
  const currentLang = i18n.language.split('-')[0];

  const defaultHourType = hourTypes.find(t => t.isDefault) || hourTypes[0];

  // Today in your own time zone (not UTC, which is still "yesterday" just after midnight).
  const today = localToday();
  const proposed = scheduledTimes(schedule, today) ?? FALLBACK_TIMES;
  const [values, setValues] = useState<TimeEntryFormValues>({
    hourTypeId: initial?.hourTypeId ?? defaultHourType?.id ?? "",
    projectId: initial?.projectId ?? "", // Added this
    workDate: initial?.workDate ?? today,
    startTime: initial?.startTime.slice(0, 5) ?? proposed.startTime,
    endTime: initial?.endTime.slice(0, 5) ?? proposed.endTime,
    breakMinutes: initial?.breakMinutes ?? proposed.breakMinutes,
    notes: initial?.notes ?? "",
    location: initial?.location ?? "",
    latitude: initial?.latitude ?? null,
    longitude: initial?.longitude ?? null
  });
  // A new entry follows the schedule for its date until you change the times yourself;
  // an existing entry keeps its own times.
  const timesEdited = useRef(!!initial);
  const followSchedule = (workDate: string) => {
    if (timesEdited.current) return;
    const times = scheduledTimes(schedule, workDate);
    if (times) setValues((v) => ({ ...v, ...times }));
  };

  // The schedule may arrive after the form opened.
  useEffect(() => {
    followSchedule(values.workDate);
    // Only when the schedule itself changes; date changes are handled where they happen.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [schedule]);

  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [locating, setLocating] = useState(false);
  const [locationError, setLocationError] = useState<string | null>(null);

  async function handleUseMyLocation() {
    setLocationError(null);
    setLocating(true);
    try {
      const coords = await getCurrentPosition();
      const address = await reverseGeocode(coords, currentLang);
      setValues((v) => ({ ...v, location: address ?? formatCoordinates(coords), ...coords }));
    } catch (err) {
      const code = err instanceof GeolocationError ? err.code : "unavailable";
      setLocationError(t(`timeEntryForm.locationError.${code}`));
    } finally {
      setLocating(false);
    }
  }

  const dateLocked = !!lockedMonths && isInLockedMonth(lockedMonths, values.workDate);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setSubmitting(true);
    try {
      await onSubmit(values);
    } catch (err) {
      setError(extractErrorMessage(err) || t('timeEntryForm.errorSave'));
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
            {hourTypes.map((type) => {
              const displayName = type.localizedNames?.[currentLang] || type.localizedNames?.['en'] || 'Unknown';
              return (
                <option key={type.id} value={type.id}>
                  {displayName}
                </option>
              );
            })}
          </select>
        </label>

        {/* NEW PROJECT DROPDOWN */}
        <label className="form__field">
          <span>{t('timeEntryForm.project')}</span>
          <select
            value={values.projectId}
            onChange={(e) => setValues({ ...values, projectId: e.target.value })}
          >
            <option value="">{t('timeEntryForm.noProject')}</option>
            {projects.map((p) => (
              <option key={p.id} value={p.id}>
                {p.name}
              </option>
            ))}
          </select>
        </label>

        <label className="form__field">
          <span>{t('timeEntryForm.date')}</span>
          <input
            type="date"
            value={values.workDate}
            onChange={(e) => {
              setValues({ ...values, workDate: e.target.value });
              followSchedule(e.target.value);
            }}
            required
          />
        </label>
        <label className="form__field">
          <span>{t('timeEntryForm.start')}</span>
          <input
            type="time"
            value={values.startTime}
            onChange={(e) => {
              timesEdited.current = true;
              setValues({ ...values, startTime: e.target.value });
            }}
            required
          />
        </label>
        <label className="form__field">
          <span>{t('timeEntryForm.end')}</span>
          <input
            type="time"
            value={values.endTime}
            onChange={(e) => {
              timesEdited.current = true;
              setValues({ ...values, endTime: e.target.value });
            }}
            required
          />
        </label>
        <label className="form__field form__field--narrow">
          <span>{t('timeEntryForm.break')}</span>
          <input
            type="number"
            min={0}
            value={values.breakMinutes}
            onChange={(e) => {
              timesEdited.current = true;
              setValues({ ...values, breakMinutes: Number(e.target.value) });
            }}
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

      <label className="form__field">
        <span>{t('timeEntryForm.location')}</span>
        <div className="form__input-group">
          <input
            type="text"
            maxLength={500}
            value={values.location}
            // Typing a different place means the stored GPS coordinates no longer apply.
            onChange={(e) => setValues({ ...values, location: e.target.value, latitude: null, longitude: null })}
            placeholder={t('timeEntryForm.locationPlaceholder')}
          />
          <button type="button" className="btn btn--ghost" onClick={handleUseMyLocation} disabled={locating}>
            {locating ? t('timeEntryForm.locating') : t('timeEntryForm.useMyLocation')}
          </button>
        </div>
      </label>
      {locationError && <p className="form__error">{locationError}</p>}

      {dateLocked && <p className="form__error">{t('lockedMonths.dateLocked')}</p>}
      {error && <p className="form__error">{error}</p>}

      <div className="form__actions">
        <button type="submit" className="btn btn--primary" disabled={submitting || !values.hourTypeId || dateLocked}>
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