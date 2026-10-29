import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { getTeamHoursOverview } from "../api/hoursOverviewApi";
import { hoursToHm } from "../utils/dateRange";
import { diffTone, localToday, signedHm } from "../utils/hoursOverview";
import type { HoursPeriod, TeamMemberHours } from "../types";

/** Worked versus scheduled hours per person. Shows nothing for people who manage nobody. */
export function TeamHoursTable() {
  const { t, i18n } = useTranslation();
  const [rows, setRows] = useState<TeamMemberHours[]>([]);

  useEffect(() => {
    let cancelled = false;
    getTeamHoursOverview(localToday())
      .then((data) => !cancelled && setRows(data))
      .catch(() => !cancelled && setRows([]));
    return () => {
      cancelled = true;
    };
  }, []);

  if (rows.length === 0) return null;

  const monthName = new Date().toLocaleDateString(i18n.language, { month: "long" });

  return (
    <div className="panel">
      <div className="page__header" style={{ marginBottom: '0.5rem' }}>
        <h2>{t('hoursOverview.teamTitle')}</h2>
        <span className="form__hint">{t('hoursOverview.teamHint')}</span>
      </div>
      <div className="table-scroll">
        <table className="list-view">
          <thead>
            <tr>
              <th>{t('leave.table.employee')}</th>
              <th>{t('hoursOverview.thisWeek')}</th>
              <th>{monthName.charAt(0).toUpperCase() + monthName.slice(1)}</th>
              <th>{t('hoursOverview.yearSoFar', { year: new Date().getFullYear() })}</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r) => (
              <tr key={r.userId}>
                <td>
                  {r.fullName}
                  <span className="list-view__notes">
                    {" · "}
                    {r.scheduleSource === "Personal"
                      ? t('hoursOverview.perWeekOwn', { hours: hoursToHm(r.weeklyScheduleHours) })
                      : t('hoursOverview.perWeek', { hours: hoursToHm(r.weeklyScheduleHours) })}
                  </span>
                </td>
                <DiffCell period={r.week} weekly={r.weeklyScheduleHours} />
                <DiffCell period={r.month} weekly={r.weeklyScheduleHours} />
                <DiffCell period={r.year} weekly={r.weeklyScheduleHours} />
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function DiffCell({ period, weekly }: { period: HoursPeriod; weekly: number }) {
  const { t } = useTranslation();
  return (
    <td title={t('hoursOverview.workedOf', { worked: hoursToHm(period.workedHours), expected: hoursToHm(period.expectedHours) })}>
      <span className={`diff-text--${diffTone(period.differenceHours, weekly)}`}>{signedHm(period.differenceHours)}</span>
    </td>
  );
}
