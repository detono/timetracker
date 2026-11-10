import { Fragment } from "react";
import { useTranslation } from "react-i18next";
import type { TimeEntry } from "../types";
import { formatDayLabel, hoursToHm } from "../utils/dateRange";

interface Props {
  days: string[];
  entries: TimeEntry[];
  onEdit?: (entry: TimeEntry) => void;
  /** Entries in a locked month can't be opened for editing. */
  isLocked?: (entry: TimeEntry) => boolean;
  /** Public holidays and company days off in view, as date → name. */
  holidays?: Record<string, string>;
}

/**
 * Renders a week-at-a-glance planboard: one row per employee present in `entries`,
 * one column per day, with a card per logged shift. Falls back to a single "You" row
 * when only the current user's entries are supplied.
 */
export function PlanBoard({ days, entries, onEdit, isLocked, holidays = {} }: Props) {
  const { t, i18n } = useTranslation();
  const currentLang = i18n.language.split('-')[0];

  const employeeNames = Array.from(new Set(entries.map((e) => e.userFullName))).sort();

  const hasHolidays = days.some((day) => holidays[day]);
  const empty = <p className="empty-state">{t('planBoard.emptyState')}</p>;

  // Without hours there are no rows, but a holiday that week is still worth showing.
  if (employeeNames.length === 0 && !hasHolidays) {
    return empty;
  }

  return (
    <div className="planboard">
      <div className="planboard__grid" style={{ gridTemplateColumns: `160px repeat(${days.length}, 1fr)` }}>
        <div className="planboard__corner" />
        {days.map((day) => (
          <div
            className={holidays[day] ? "planboard__day-header planboard__day-header--holiday" : "planboard__day-header"}
            key={day}
          >
            {formatDayLabel(day)}
            {holidays[day] && (
              <span className="planboard__holiday" title={holidays[day]}>
                {holidays[day]}
              </span>
            )}
          </div>
        ))}

        {employeeNames.map((name) => (
          <Fragment key={name}>
            <div className="planboard__row-label" key={`label-${name}`}>
              {name}
            </div>
            {days.map((day) => {
              const cellEntries = entries.filter((e) => e.userFullName === name && e.workDate === day);
              return (
                <div
                  className={holidays[day] ? "planboard__cell planboard__cell--holiday" : "planboard__cell"}
                  key={`${name}-${day}`}
                >
                  {cellEntries.map((entry) => {
                    const locked = isLocked?.(entry) ?? false;
                    return (
                    <button
                      key={entry.id}
                      className={locked ? "planboard__shift planboard__shift--locked" : "planboard__shift"}
                      style={{
                        borderLeftColor: entry.hourTypeColor,
                        backgroundColor: `${entry.hourTypeColor}22`
                      }}
                      onClick={() => !locked && onEdit?.(entry)}
                      title={locked ? t('lockedMonths.lockedHint') : entry.notes ?? undefined}
                    >
                      <span className="planboard__shift-time">
                        {entry.startTime.slice(0, 5)}–{entry.endTime.slice(0, 5)}
                      </span>
                      <span className="planboard__shift-type">
                        {/* Resolve the dictionary right here in the component */}
                        {entry.localizedHourTypeNames?.[currentLang] || entry.localizedHourTypeNames?.['en'] || 'Unknown'}
                      </span>
                      <span className="planboard__shift-duration">
                        {locked && "🔒 "}
                        {hoursToHm(entry.durationHours)}
                      </span>
                    </button>
                    );
                  })}
                </div>
              );
            })}
          </Fragment>
        ))}
      </div>
      {employeeNames.length === 0 && empty}
    </div>
  );
}