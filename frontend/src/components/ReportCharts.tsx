import { useTranslation } from "react-i18next";
import type { CSSProperties, ReactNode } from "react";
import type { HoursBalanceLine, HoursReport, LeaveBalance, ProjectHoursLine } from "../types";
import { hoursToHm } from "../utils/dateRange";
import { localToday, signedHm } from "../utils/hoursOverview";
import {
  billableShare,
  donutBackground,
  hourTypeTotals,
  periodTotals,
  personBalances,
  shortPeriodLabel
} from "../utils/reportCharts";

const LEAVE_PENDING = "var(--warn)";
const LEAVE_LEFT = "var(--line)";
const NOT_BILLABLE = "var(--ink-soft)";

interface ReportChartsProps {
  report: HoursReport;
  /** Empty with a project filter: worked vs. expected is about all of someone's hours. */
  balance: HoursBalanceLine[];
  projectHours: ProjectHoursLine[];
  /** Null when leave couldn't be loaded; the leave parts are then left out. */
  leave: LeaveBalance[] | null;
  leaveYear: number;
}

/** The report's numbers as charts: the same data, filters and scope as the tables. */
export function ReportCharts({ report, balance, projectHours, leave, leaveYear }: ReportChartsProps) {
  const { t } = useTranslation();

  if (report.lines.length === 0) {
    return <p className="empty-state">{t('reports.emptyState')}</p>;
  }

  const people = personBalances(balance);
  const expected = balance.length > 0 ? people.reduce((sum, p) => sum + p.expected, 0) : null;
  const difference = people.reduce((sum, p) => sum + p.difference, 0);
  const share = billableShare(projectHours);
  const leaveWithAllowance = (leave ?? []).filter((l) => l.allowanceHours !== null || l.usedHours > 0 || l.pendingHours > 0);

  return (
    <div className="report-charts">
      <div className="report-charts__kpis">
        <Kpi label={t('reports.charts.logged')} value={hoursToHm(report.grandTotalHours)}
          note={expected !== null ? t('reports.charts.ofScheduled', { hours: hoursToHm(expected) }) : undefined} />
        {expected !== null && (
          <Kpi
            label={t('reports.charts.balance')}
            value={signedHm(difference)}
            tone={difference <= -0.25 ? "behind" : difference >= 0.25 ? "plus" : undefined}
            note={t(difference <= -0.25 ? 'reports.charts.behind' : difference >= 0.25 ? 'reports.charts.ahead' : 'reports.charts.even')}
          />
        )}
        {share && (
          <Kpi
            label={t('reports.charts.billable')}
            value={percent(share.billable, share.total)}
            note={t('reports.charts.billableHours', { hours: hoursToHm(share.billable) })}
            ring={<Donut size="small" label={t('reports.charts.billableRing')}
              parts={[{ value: share.billable, color: "var(--color-primary)" }, { value: share.total - share.billable, color: LEAVE_LEFT }]} />}
          />
        )}
        {leaveWithAllowance.length === 1 && <MyLeaveKpi leave={leaveWithAllowance[0]} />}
        {leaveWithAllowance.length > 1 && (
          <Kpi
            label={t('reports.charts.leaveTaken', { year: leaveYear })}
            value={hoursToHm(sum(leaveWithAllowance, (l) => l.usedHours))}
            note={t('reports.charts.leavePending', { hours: hoursToHm(sum(leaveWithAllowance, (l) => l.pendingHours)) })}
          />
        )}
      </div>

      <PeriodChart report={report} balance={balance} />

      <div className="report-charts__grid">
        {share && <ProjectBars lines={projectHours} />}
        <HourTypeDonut report={report} />
      </div>

      {(people.length > 1 || leaveWithAllowance.length > 0) && (
        <div className="report-charts__grid">
          {people.length > 1 && <PersonBalanceBars people={people} />}
          {leaveWithAllowance.length > 1 && <LeaveBars leave={leaveWithAllowance} year={leaveYear} />}
          {leaveWithAllowance.length === 1 && <MyLeaveDonut leave={leaveWithAllowance[0]} year={leaveYear} />}
        </div>
      )}
    </div>
  );
}

function Kpi({ label, value, note, tone, ring }: {
  label: string; value: string; note?: string; tone?: "plus" | "behind"; ring?: JSX.Element;
}) {
  return (
    <div className="report-charts__card report-charts__kpi">
      <div>
        <div className="report-charts__kpi-label">{label}</div>
        <div className={tone ? `report-charts__kpi-value report-charts__kpi-value--${tone}` : "report-charts__kpi-value"}>{value}</div>
        {note && <div className="report-charts__kpi-note">{note}</div>}
      </div>
      {ring}
    </div>
  );
}

function MyLeaveKpi({ leave }: { leave: LeaveBalance }) {
  const { t } = useTranslation();
  if (leave.remainingHours === null) {
    return <Kpi label={t('reports.charts.leaveTaken', { year: leave.year })} value={hoursToHm(leave.usedHours)} note={t('reports.charts.noAllowance')} />;
  }
  return (
    <Kpi
      label={t('reports.charts.leaveLeft', { year: leave.year })}
      value={leave.remainingHours < 0 ? signedHm(leave.remainingHours) : hoursToHm(leave.remainingHours)}
      note={t('reports.charts.leaveUsedPending', { used: hoursToHm(leave.usedHours), pending: hoursToHm(leave.pendingHours) })}
    />
  );
}

/** Worked per period, with the schedule as a line on each bar; the period that holds today is still running. */
function PeriodChart({ report, balance }: { report: HoursReport; balance: HoursBalanceLine[] }) {
  const { t, i18n } = useTranslation();
  const periods = periodTotals(report.lines, balance);
  const hasSchedule = periods.some((p) => p.expected !== null);
  const max = Math.max(...periods.map((p) => Math.max(p.worked, p.expected ?? 0)), 1);
  const today = localToday();
  const pct = (hours: number) => `${(hours / max) * 100}%`;

  return (
    <section className="report-charts__card" aria-labelledby="report-charts-periods">
      <div className="report-charts__head">
        <div>
          <h2 id="report-charts-periods" className="report-charts__title">
            {hasSchedule ? t('reports.charts.periodsTitle') : t('reports.charts.periodsTitleWorked')}
          </h2>
          {hasSchedule && <p className="form__hint">{t('reports.charts.periodsHint')}</p>}
        </div>
        <div className="report-charts__legend">
          <span><span className="report-charts__swatch" style={{ background: "var(--color-primary)" }} />{t('reports.charts.worked')}</span>
          <span><span className="report-charts__swatch report-charts__swatch--running" />{t('reports.charts.running')}</span>
          {hasSchedule && <span><span className="report-charts__swatch report-charts__swatch--line" />{t('reports.charts.scheduled')}</span>}
        </div>
      </div>
      <div className="report-charts__scroll">
        <ol className="report-charts__columns" style={{ "--columns": periods.length } as CSSProperties}>
          {periods.map((p) => {
            const running = p.start <= today && today <= p.end;
            const label = shortPeriodLabel(p.start, report.grouping, i18n.language);
            const summary = p.expected !== null
              ? t('reports.charts.periodSummary', { period: label, worked: hoursToHm(p.worked), expected: hoursToHm(p.expected) })
              : `${label}: ${hoursToHm(p.worked)}`;
            return (
              <li key={p.start} className="report-charts__column" title={summary}>
                <span className="report-charts__column-value">{hoursToHm(p.worked)}</span>
                <span className="report-charts__plot" aria-hidden="true">
                  <span className={running ? "report-charts__bar report-charts__bar--running" : "report-charts__bar"} style={{ height: pct(p.worked) }} />
                  {p.expected !== null && p.expected > 0 && <span className="report-charts__target" style={{ bottom: pct(p.expected) }} />}
                </span>
                <span className="report-charts__column-label">{label}</span>
                <span className="visually-hidden">{summary}</span>
              </li>
            );
          })}
        </ol>
      </div>
    </section>
  );
}

function ProjectBars({ lines }: { lines: ProjectHoursLine[] }) {
  const { t } = useTranslation();
  const total = sum(lines, (l) => l.totalHours);
  const max = Math.max(...lines.map((l) => l.totalHours), 1);

  return (
    <section className="report-charts__card" aria-labelledby="report-charts-projects">
      <h2 id="report-charts-projects" className="report-charts__title">{t('reports.projectsTitle')}</h2>
      <p className="form__hint">{t('reports.charts.projectsHint')}</p>
      <ul className="report-charts__bars">
        {[...lines].sort((a, b) => b.totalHours - a.totalHours).map((line) => (
          <li key={line.projectId ?? "none"}>
            <div className="report-charts__bar-row">
              <span>
                <strong>{line.projectName ?? t('reports.noProject')}</strong>
                {line.clientName && <span className="report-charts__muted"> {line.clientName}</span>}
              </span>
              <span className="report-charts__muted report-charts__num">{hoursToHm(line.totalHours)} · {percent(line.totalHours, total)}</span>
            </div>
            <span className="report-charts__track" aria-hidden="true">
              <span style={{ width: `${(line.totalHours / max) * 100}%`, background: line.projectId && line.isBillable ? "var(--color-primary)" : NOT_BILLABLE }} />
            </span>
          </li>
        ))}
      </ul>
    </section>
  );
}

function HourTypeDonut({ report }: { report: HoursReport }) {
  const { t } = useTranslation();
  const types = hourTypeTotals(report.lines);

  return (
    <section className="report-charts__card" aria-labelledby="report-charts-types">
      <h2 id="report-charts-types" className="report-charts__title">{t('reports.charts.typesTitle')}</h2>
      <p className="form__hint">{t('reports.charts.typesHint')}</p>
      <div className="report-charts__donut-row">
        <Donut label={t('reports.charts.typesTitle')} parts={types.map((type) => ({ value: type.hours, color: type.color }))}>
          <strong>{hoursToHm(report.grandTotalHours)}</strong>
          <span>{t('reports.charts.loggedShort')}</span>
        </Donut>
        <ul className="report-charts__legend-list">
          {types.map((type) => (
            <li key={type.id}>
              <span className="report-charts__swatch" style={{ background: type.color }} />
              <span className="report-charts__legend-name">{type.name}</span>
              <span className="report-charts__num">{hoursToHm(type.hours)}</span>
              <span className="report-charts__muted report-charts__num report-charts__pct">{percent(type.hours, report.grandTotalHours)}</span>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}

/** Behind to the left, ahead to the right of a middle line, all on one scale. */
function PersonBalanceBars({ people }: { people: ReturnType<typeof personBalances> }) {
  const { t } = useTranslation();
  const max = Math.max(...people.map((p) => Math.abs(p.difference)), 1);
  const width = (hours: number) => `${(Math.abs(hours) / max) * 100}%`;

  return (
    <section className="report-charts__card" aria-labelledby="report-charts-people">
      <h2 id="report-charts-people" className="report-charts__title">{t('reports.charts.peopleTitle')}</h2>
      <p className="form__hint">{t('reports.charts.peopleHint')}</p>
      <ul className="report-charts__people">
        {people.map((p) => (
          <li key={p.userId}>
            <span className="report-charts__name">{p.name}</span>
            <span className="report-charts__diverging" aria-hidden="true">
              <span>{p.difference < 0 && <span className="report-charts__behind" style={{ width: width(p.difference) }} />}</span>
              <span>{p.difference > 0 && <span className="report-charts__ahead" style={{ width: width(p.difference) }} />}</span>
            </span>
            <span className={`report-charts__num report-charts__diff${p.difference < 0 ? " report-charts__diff--behind" : p.difference > 0 ? " report-charts__diff--plus" : ""}`}>
              {signedHm(p.difference)}
            </span>
          </li>
        ))}
      </ul>
    </section>
  );
}

function LeaveLegend() {
  const { t } = useTranslation();
  return (
    <div className="report-charts__legend">
      <span><span className="report-charts__swatch" style={{ background: "var(--color-primary)" }} />{t('reports.charts.used')}</span>
      <span><span className="report-charts__swatch" style={{ background: LEAVE_PENDING }} />{t('reports.charts.pending')}</span>
      <span><span className="report-charts__swatch" style={{ background: LEAVE_LEFT }} />{t('reports.charts.left')}</span>
    </div>
  );
}

/** "17h left", or "8h over the allowance" for someone who took more than they had. */
function useLeftText() {
  const { t } = useTranslation();
  return (remaining: number) =>
    remaining < 0
      ? t('reports.charts.overAllowance', { hours: hoursToHm(-remaining) })
      : t('reports.charts.leftHours', { hours: hoursToHm(remaining) });
}

function LeaveBars({ leave, year }: { leave: LeaveBalance[]; year: number }) {
  const { t } = useTranslation();
  const leftText = useLeftText();
  // People without an allowance are drawn on the biggest allowance's scale.
  const scale = Math.max(...leave.map((l) => l.allowanceHours ?? l.usedHours + l.pendingHours), 1);

  return (
    <section className="report-charts__card" aria-labelledby="report-charts-leave">
      <h2 id="report-charts-leave" className="report-charts__title">{t('reports.charts.leaveTitle', { year })}</h2>
      <p className="form__hint">{t('reports.charts.leaveHint')}</p>
      <LeaveLegend />
      <ul className="report-charts__people">
        {leave.map((l) => {
          const of = l.allowanceHours ?? scale;
          return (
            <li key={l.userId}>
              <span className="report-charts__name">{l.userFullName}</span>
              <span className="report-charts__stack" aria-hidden="true">
                <span style={{ width: `${Math.min(l.usedHours / of, 1) * 100}%`, background: "var(--color-primary)" }} />
                <span style={{ width: `${Math.min(l.pendingHours / of, 1) * 100}%`, background: LEAVE_PENDING }} />
              </span>
              <span className="report-charts__muted report-charts__num report-charts__leave-left">
                {l.remainingHours === null ? t('reports.charts.noAllowance') : leftText(l.remainingHours)}
              </span>
            </li>
          );
        })}
      </ul>
    </section>
  );
}

function MyLeaveDonut({ leave, year }: { leave: LeaveBalance; year: number }) {
  const { t } = useTranslation();
  const remaining = leave.remainingHours ?? 0;
  const left = Math.max(remaining, 0);

  return (
    <section className="report-charts__card" aria-labelledby="report-charts-leave">
      <h2 id="report-charts-leave" className="report-charts__title">{t('reports.charts.leaveTitle', { year })}</h2>
      <p className="form__hint">
        {leave.allowanceHours !== null ? t('reports.charts.allowanceOf', { hours: hoursToHm(leave.allowanceHours) }) : t('reports.charts.noAllowance')}
      </p>
      <div className="report-charts__donut-row">
        <Donut label={t('reports.charts.leaveTitle', { year })} parts={[
          { value: leave.usedHours, color: "var(--color-primary)" },
          { value: leave.pendingHours, color: LEAVE_PENDING },
          { value: Math.max(left, 0), color: LEAVE_LEFT }
        ]}>
          <strong>{leave.remainingHours === null ? hoursToHm(leave.usedHours) : remaining < 0 ? signedHm(remaining) : hoursToHm(left)}</strong>
          <span>{leave.remainingHours === null ? t('reports.charts.used') : remaining < 0 ? t('reports.charts.over') : t('reports.charts.left')}</span>
        </Donut>
        <ul className="report-charts__legend-list">
          <li><span className="report-charts__swatch" style={{ background: "var(--color-primary)" }} /><span className="report-charts__legend-name">{t('reports.charts.used')}</span><span className="report-charts__num">{hoursToHm(leave.usedHours)}</span></li>
          <li><span className="report-charts__swatch" style={{ background: LEAVE_PENDING }} /><span className="report-charts__legend-name">{t('reports.charts.pending')}</span><span className="report-charts__num">{hoursToHm(leave.pendingHours)}</span></li>
          {leave.remainingHours !== null && (
            <li><span className="report-charts__swatch" style={{ background: LEAVE_LEFT }} /><span className="report-charts__legend-name">{t('reports.charts.left')}</span><span className="report-charts__num">{hoursToHm(left)}</span></li>
          )}
        </ul>
      </div>
    </section>
  );
}

/** A ring drawn with a conic gradient; the children sit in the hole. */
function Donut({ parts, label, size = "large", children }: {
  parts: { value: number; color: string }[]; label: string; size?: "large" | "small"; children?: ReactNode;
}) {
  return (
    <div className={`report-charts__donut report-charts__donut--${size}`} role="img" aria-label={label}
      style={{ background: donutBackground(parts) }}>
      <div className="report-charts__donut-hole">{children}</div>
    </div>
  );
}

const sum = <T,>(items: T[], pick: (item: T) => number) => items.reduce((total, item) => total + pick(item), 0);
const percent = (part: number, whole: number) => (whole > 0 ? `${Math.round((part / whole) * 100)}%` : "0%");
