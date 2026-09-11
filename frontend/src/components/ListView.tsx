import type { TimeEntry } from "../types";
import { formatDayLabel, hoursToHm } from "../utils/dateRange";

interface Props {
  entries: TimeEntry[];
  showEmployeeColumn?: boolean;
  onEdit?: (entry: TimeEntry) => void;
  onDelete?: (entry: TimeEntry) => void;
}

export function ListView({ entries, showEmployeeColumn, onEdit, onDelete }: Props) {
  if (entries.length === 0) {
    return <p className="empty-state">No hours logged for this period yet.</p>;
  }

  return (
    <table className="list-view">
      <thead>
        <tr>
          {showEmployeeColumn && <th>Employee</th>}
          <th>Date</th>
          <th>Start</th>
          <th>End</th>
          <th>Break</th>
          <th>Duration</th>
          <th>Notes</th>
          {(onEdit || onDelete) && <th aria-label="Actions" />}
        </tr>
      </thead>
      <tbody>
        {entries.map((entry) => (
          <tr key={entry.id}>
            {showEmployeeColumn && <td>{entry.userFullName}</td>}
            <td>{formatDayLabel(entry.workDate)}</td>
            <td>{entry.startTime.slice(0, 5)}</td>
            <td>{entry.endTime.slice(0, 5)}</td>
            <td>{entry.breakMinutes} min</td>
            <td className="list-view__duration">{hoursToHm(entry.durationHours)}</td>
            <td className="list-view__notes">{entry.notes ?? "—"}</td>
            {(onEdit || onDelete) && (
              <td className="list-view__actions">
                {onEdit && (
                  <button className="btn btn--ghost btn--sm" onClick={() => onEdit(entry)}>
                    Edit
                  </button>
                )}
                {onDelete && (
                  <button className="btn btn--ghost btn--sm btn--danger" onClick={() => onDelete(entry)}>
                    Delete
                  </button>
                )}
              </td>
            )}
          </tr>
        ))}
      </tbody>
    </table>
  );
}
