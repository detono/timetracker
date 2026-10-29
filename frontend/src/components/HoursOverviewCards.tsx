import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { getMyHoursOverview } from "../api/hoursOverviewApi";
import { getLeaveBalances } from "../api/leaveApi";
import { useAuth } from "../auth/AuthContext";
import { hoursToHm } from "../utils/dateRange";
import { diffTone, localToday, shortHm, signedHm } from "../utils/hoursOverview";
import type { HoursDay, HoursPeriod, LeaveBalance, MyHoursOverview } from "../types";

interface Props {
  /** Change this to reload, e.g. after logging hours. */
  reloadKey: number;
}

/** "Am I working enough?": this week, month and year so far against the work schedule. */
export function HoursOverviewCards({ reloadKey }: Props) {
  const { t, i18n } = useTranslation();
  const { user } = useAuth();
  const [overview, setOverview] = useState<MyHoursOverview | null>(null);
  const [leave, setLeave] = useState<LeaveBalance | null>(null);

  useEffect(() => {
    let cancelled = false;
    getMyHoursOverview(localToday())
      .then((data) => !cancelled && setOverview(data))
      .catch(() => !cancelled && setOverview(null));
    // Your own leave balance for this year, when an allowance was set. Employers don't get one.
    getLeaveBalances(new Date().getFullYear())
      .then((balances) => !cancelled && setLeave(balances.find((b) => b.userId === user?.userId) ?? null))
      .catch(() => !cancelled && setLeave(null));
    return () => {
      cancelled = true;
    };
  }, [reloadKey, user?.userId]);

  if (!overview || !overview.tracksHours) return null;

  const asOf = new Date(overview.asOf + "T00:00:00");
  const monthName = asOf.toLocaleDateString(i18n.language, { month: "long" });
  const visibleDays = overview.days.filter((d) => d.expectedHours > 0 || d.workedHours > 0);

  return (
    <section className="hours-overview" aria-label={t('hoursOverview.title')}>
      <div className="hours-overview__cards">
        <PeriodCard label={t('hoursOverview.thisWeek')} period={overview.week} weekly={overview.weeklyScheduleHours} />
        <PeriodCard
          label={monthName.charAt(0).toUpperCase() + monthName.slice(1)}
          period={overview.month}
          weekly={overview.weeklyScheduleHours}
        />
        <PeriodCard
          label={t('hoursOverview.yearSoFar', { year: asOf.getFullYear() })}
          period={overview.year}
          weekly={overview.weeklyScheduleHours}
        />
        {leave?.allowanceHours != null && leave.remainingHours != null && (
          <div className="hours-card">
            <span className="hours-card__label">{t('hoursOverview.leaveLeft')}</span>
            <span className="hours-card__value">
              {hoursToHm(leave.remainingHours)}{" "}
              <span className="hours-card__of">{t('hoursOverview.of', { hours: hoursToHm(leave.allowanceHours) })}</span>
            </span>
            <Progress value={leave.remainingHours} max={leave.allowanceHours} />
            {leave.pendingHours > 0 && (
              <span className="hours-card__note">{t('hoursOverview.leavePending', { hours: hoursToHm(leave.pendingHours) })}</span>
            )}
          </div>
        )}
      </div>

      {visibleDays.length > 0 && (
        <div className="hours-overview__days">
          {visibleDays.map((day) => (
            <DayCell key={day.date} day={day} language={i18n.language} />
          ))}
        </div>
      )}
      <p className="form__hint">{t('hoursOverview.hint', { hours: hoursToHm(overview.weeklyScheduleHours) })}</p>
    </section>
  );
}

function DiffBadge({ difference, weekly }: { difference: number; weekly: number }) {
  const { t } = useTranslation();
  const tone = diffTone(difference, weekly);
  const text =
    tone === "even"
      ? t('hoursOverview.onTarget')
      : tone === "plus"
        ? signedHm(difference)
        : t('hoursOverview.short', { hours: shortHm(difference) });
  return <span className={`diff diff--${tone}`}>{text}</span>;
}

function PeriodCard({ label, period, weekly }: { label: string; period: HoursPeriod; weekly: number }) {
  const { t } = useTranslation();
  return (
    <div className="hours-card">
      <span className="hours-card__label">{label}</span>
      <span className="hours-card__value">
        {hoursToHm(period.workedHours)}{" "}
        <span className="hours-card__of">{t('hoursOverview.of', { hours: hoursToHm(period.expectedHours) })}</span>
      </span>
      <Progress value={period.workedHours} max={period.expectedHours} />
      <DiffBadge difference={period.differenceHours} weekly={weekly} />
    </div>
  );
}

/** A thin bar showing how far along a value is; nothing when there's no maximum. */
function Progress({ value, max }: { value: number; max: number }) {
  if (max <= 0) return null;
  const percent = Math.max(0, Math.min(100, Math.round((value / max) * 100)));
  return (
    <span className="progress" aria-hidden="true">
      <span className="progress__bar" style={{ width: `${percent}%` }} />
    </span>
  );
}

function DayCell({ day, language }: { day: HoursDay; language: string }) {
  const { t } = useTranslation();
  const label = new Date(day.date + "T00:00:00").toLocaleDateString(language, { weekday: "short" });
  const difference = day.workedHours - day.expectedHours;

  let status: JSX.Element;
  if (day.holidayName && day.workedHours === 0) {
    status = <span className="hours-day__note" title={day.holidayName}>{day.holidayName}</span>;
  } else if (day.isPending) {
    status = day.hasLeave
      ? <span className="hours-day__note hours-day__note--leave">{t('hoursOverview.leaveBooked')}</span>
      : <span className="hours-day__note">{t('hoursOverview.notYet')}</span>;
  } else if (day.expectedHours === 0) {
    status = <span className="hours-day__note">{t('hoursOverview.dayOff')}</span>;
  } else {
    const tone = diffTone(difference, 40);
    status = (
      <span className={`hours-day__note diff-text--${tone}`}>
        {tone === "even" ? t('hoursOverview.onTarget') : tone === "plus" ? signedHm(difference) : t('hoursOverview.short', { hours: shortHm(difference) })}
      </span>
    );
  }

  return (
    <div className={day.isPending ? "hours-day hours-day--pending" : "hours-day"}>
      <span className="hours-day__name">{label}</span>
      <span className="hours-day__value">
        {day.isPending && !day.hasLeave ? "—" : hoursToHm(day.workedHours)}
        <span className="hours-card__of"> / {hoursToHm(day.expectedHours)}</span>
      </span>
      {status}
    </div>
  );
}
