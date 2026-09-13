import { useTranslation } from "react-i18next";
import type { TimeEntry } from "../types";
import { formatDayLabel, hoursToHm } from "../utils/dateRange";
import { HourTypeBadge } from "./HourTypeBadge";

interface Props {
  entries: TimeEntry[];
  showEmployeeColumn?: boolean;
  onEdit?: (entry: TimeEntry) => void;
  onDelete?: (entry: TimeEntry) => void;
}

export function ListView({ entries, showEmployeeColumn, onEdit, onDelete }: Props) {
  const { t } = useTranslation();

  if (entries.length === 0) {
    return <p className="empty-state">{t('listView.emptyState')}</p>;
  }

  return (
    <table className="list-view">
      <thead>
        <tr>
          {showEmployeeColumn && <th>{t('listView.employee')}</th>}
          <th>{t('listView.type')}</th>
          <th>{t('listView.date')}</th>
          <th>{t('listView.start')}</th>
          <th>{t('listView.end')}</th>
          <th>{t('listView.break')}</th>
          <th>{t('listView.duration')}</th>
          <th>{t('listView.notes')}</th>
          <th>{t('timeEntryForm.project')}</th>
          {(onEdit || onDelete) && <th aria-label={t('listView.actions')} />}
        </tr>
      </thead>
      <tbody>
        {entries.map((entry) => (
          <tr key={entry.id}>
            {showEmployeeColumn && <td>{entry.userFullName}</td>}
            <td>
              <HourTypeBadge name={entry.hourTypeName} colorHex={entry.hourTypeColor} />
            </td>
            <td>{formatDayLabel(entry.workDate)}</td>
            <td>{entry.startTime.slice(0, 5)}</td>
            <td>{entry.endTime.slice(0, 5)}</td>
            <td>{entry.breakMinutes} {t('listView.minutes')}</td>
            <td className="list-view__duration">{hoursToHm(entry.durationHours)}</td>
            <td className="list-view__notes">{entry.notes ?? "—"}</td>
            <td>
              {entry.projectName ? (
                <span className="badge badge--neutral">{entry.projectName}</span>
              ) : (
                <span className="list-view__notes">—</span>
              )}
            </td>
            {(onEdit || onDelete) && (
              <td className="list-view__actions">
                {onEdit && (
                  <button className="btn btn--ghost btn--sm" onClick={() => onEdit(entry)}>
                    {t('common.edit')}
                  </button>
                )}
                {onDelete && (
                  <button className="btn btn--ghost btn--sm btn--danger" onClick={() => onDelete(entry)}>
                    {t('common.delete')}
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