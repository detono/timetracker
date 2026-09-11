import { Fragment } from "react";
import type { TimeEntry } from "../types";
import { formatDayLabel, hoursToHm } from "../utils/dateRange";

interface Props {
  days: string[];
  entries: TimeEntry[];
  onEdit?: (entry: TimeEntry) => void;
}

/**
 * Renders a week-at-a-glance planboard: one row per employee present in `entries`,
 * one column per day, with a card per logged shift. Falls back to a single "You" row
 * when only the current user's entries are supplied.
 */
export function PlanBoard({ days, entries, onEdit }: Props) {
  const employeeNames = Array.from(new Set(entries.map((e) => e.userFullName))).sort();

  if (employeeNames.length === 0) {
    return <p className="empty-state">No hours logged for this period yet.</p>;
  }

  return (
    <div className="planboard">
      <div className="planboard__grid" style={{ gridTemplateColumns: `160px repeat(${days.length}, 1fr)` }}>
        <div className="planboard__corner" />
        {days.map((day) => (
          <div className="planboard__day-header" key={day}>
            {formatDayLabel(day)}
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
                <div className="planboard__cell" key={`${name}-${day}`}>
                  {cellEntries.map((entry) => (
                    <button
                      key={entry.id}
                      className="planboard__shift"
                      style={{
                        borderLeftColor: entry.hourTypeColor,
                        backgroundColor: `${entry.hourTypeColor}22`
                      }}
                      onClick={() => onEdit?.(entry)}
                      title={entry.notes ?? undefined}
                    >
                      <span className="planboard__shift-time">
                        {entry.startTime.slice(0, 5)}–{entry.endTime.slice(0, 5)}
                      </span>
                      <span className="planboard__shift-type">{entry.hourTypeName}</span>
                      <span className="planboard__shift-duration">{hoursToHm(entry.durationHours)}</span>
                    </button>
                  ))}
                </div>
              );
            })}
          </Fragment>
        ))}
      </div>
    </div>
  );
}
