import { useTranslation } from "react-i18next";
import type { TimeEntry } from "../types";
import { formatDayLabel, hoursToHm } from "../utils/dateRange";
import { HourTypeBadge } from "./HourTypeBadge";
import { mapUrl } from "../utils/geolocation";
import { RowMenu } from "./RowMenu";
import { Icon } from "./Icon";

interface Props {
  entries: TimeEntry[];
  showEmployeeColumn?: boolean;
  onEdit?: (entry: TimeEntry) => void;
  onDelete?: (entry: TimeEntry) => void;
  /** Entries in a locked month get no edit or delete buttons. */
  isLocked?: (entry: TimeEntry) => boolean;
}

export function ListView({ entries, showEmployeeColumn, onEdit, onDelete, isLocked }: Props) {
  const { t } = useTranslation();

  if (entries.length === 0) {
    return <p className="empty-state">{t('listView.emptyState')}</p>;
  }

  const actionsFor = (entry: TimeEntry) =>
    isLocked?.(entry) ? (
      <span className="locked-label" title={t('lockedMonths.lockedHint')}>
        <Icon name="lock" size={14} /> {t('lockedMonths.locked')}
      </span>
    ) : (
      <RowMenu
        items={[
          ...(onEdit ? [{ label: t('common.edit'), onSelect: () => onEdit(entry) }] : []),
          ...(onDelete ? [{ label: t('common.delete'), onSelect: () => onDelete(entry), danger: true }] : [])
        ]}
      />
    );

  return (
    <>
    {/* Phones get cards instead of the wide table; CSS shows one or the other. */}
    <ul className="entry-cards">
      {entries.map((entry) => (
        <li key={entry.id} className="entry-card">
          <div className="entry-card__top">
            <span className="entry-card__date">{formatDayLabel(entry.workDate)}</span>
            <span className="list-view__duration">{hoursToHm(entry.durationHours)}</span>
          </div>
          {showEmployeeColumn && <div className="entry-card__person">{entry.userFullName}</div>}
          <div className="entry-card__middle">
            <HourTypeBadge localizedNames={entry.localizedHourTypeNames} colorHex={entry.hourTypeColor} />
            {(onEdit || onDelete) && actionsFor(entry)}
          </div>
          <div className="entry-card__meta">
            {[
              `${entry.startTime.slice(0, 5)}–${entry.endTime.slice(0, 5)}`,
              entry.breakMinutes > 0 ? `${entry.breakMinutes} ${t('listView.minutes')} ${t('listView.break').toLowerCase()}` : null,
              entry.projectName,
              entry.location
            ]
              .filter(Boolean)
              .join(" · ")}
          </div>
          {entry.notes && <div className="entry-card__notes">{entry.notes}</div>}
        </li>
      ))}
    </ul>
    <div className="table-scroll table-scroll--wide-only">
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
            <th>{t('listView.location')}</th>
            {(onEdit || onDelete) && <th aria-label={t('listView.actions')} />}
          </tr>
        </thead>
        <tbody>
          {entries.map((entry) => (
            <tr key={entry.id}>
              {showEmployeeColumn && <td>{entry.userFullName}</td>}
              <td>
                <HourTypeBadge
                  localizedNames={entry.localizedHourTypeNames}
                  colorHex={entry.hourTypeColor}
                />
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
              <td className="list-view__notes">
                {entry.latitude != null && entry.longitude != null ? (
                  <a href={mapUrl({ latitude: entry.latitude, longitude: entry.longitude })} target="_blank" rel="noreferrer">
                    {entry.location ?? "📍"}
                  </a>
                ) : (
                  entry.location ?? "—"
                )}
              </td>
              {(onEdit || onDelete) && (
                <td className="list-view__actions">{actionsFor(entry)}</td>
              )}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
    </>
  );
}