import { useCallback, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { format, startOfMonth } from "date-fns";
import { downloadHoursBalanceCsv, downloadHoursReportCsv, getHoursBalance, getHoursReport } from "../api/reportsApi";
import { extractErrorMessage } from "../api/client";
import { getLockedMonths, lockMonth, unlockMonth } from "../api/lockedMonthsApi";
import { useAuth } from "../auth/AuthContext";
import { monthKey } from "../utils/lockedMonths";
import { formatDayLabel } from "../utils/dateRange";
import { HourTypeBadge } from "../components/HourTypeBadge";
import type { HoursBalanceLine, HoursReport, LockedMonth, ReportGrouping } from "../types";
import { hoursToHm } from "../utils/dateRange";
import { signedHm } from "../utils/hoursOverview";

export function ReportsPage() {
  const { t } = useTranslation();
  const { user } = useAuth();
  const [from, setFrom] = useState(format(startOfMonth(new Date()), "yyyy-MM-dd"));
  const [to, setTo] = useState(format(new Date(), "yyyy-MM-dd"));
  const [grouping, setGrouping] = useState<ReportGrouping>("Week");
  const [report, setReport] = useState<HoursReport | null>(null);
  const [balance, setBalance] = useState<HoursBalanceLine[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function runReport() {
    setLoading(true);
    setError(null);
    try {
      const [data, balanceData] = await Promise.all([
        getHoursReport(from, to, grouping),
        getHoursBalance(from, to, grouping)
      ]);
      setReport(data);
      setBalance(balanceData);
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }

  async function handleDownload(download: typeof downloadHoursReportCsv) {
    try {
      await download(from, to, grouping);
    } catch (err) {
      setError(extractErrorMessage(err));
    }
  }

  return (
    <div className="page">
      <div className="page__header">
        <div>
          <h1>{t('reports.title')}</h1>
          <p className="page__subtitle">{t('reports.subtitle')}</p>
        </div>
      </div>

      <div className="panel">
        <div className="form__row form__row--reports">
          <label className="form__field">
            <span>{t('reports.from')}</span>
            <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
          </label>
          <label className="form__field">
            <span>{t('reports.to')}</span>
            <input type="date" value={to} onChange={(e) => setTo(e.target.value)} />
          </label>
          <label className="form__field form__field--narrow">
            <span>{t('reports.groupBy')}</span>
            <select value={grouping} onChange={(e) => setGrouping(e.target.value as ReportGrouping)}>
              <option value="Day">{t('reports.grouping.day')}</option>
              <option value="Week">{t('reports.grouping.week')}</option>
              <option value="Month">{t('reports.grouping.month')}</option>
            </select>
          </label>
          <div className="form__actions form__actions--inline">
            <button className="btn btn--primary" onClick={runReport} disabled={loading}>
              {loading ? t('common.running') : t('reports.runReport')}
            </button>
            {report && (
              <>
                <button className="btn btn--ghost" onClick={() => handleDownload(downloadHoursReportCsv)}>
                  {t('reports.downloadCsv')}
                </button>
                <button className="btn btn--ghost" onClick={() => handleDownload(downloadHoursBalanceCsv)}>
                  {t('reports.downloadBalanceCsv')}
                </button>
              </>
            )}
          </div>
        </div>
      </div>

      {error && <p className="form__error">{error}</p>}

      {report && (
        <>
          <p className="page__subtitle">
            {t('reports.grandTotal')} <strong>{hoursToHm(report.grandTotalHours)}</strong> {t('reports.lines', { count: report.lines.length })}
          </p>
          <table className="list-view">
            <thead>
              <tr>
                <th>{t('reports.table.employee')}</th>
                <th>{t('reports.table.type')}</th>
                <th>{t('reports.table.period')}</th>
                <th>{t('reports.table.range')}</th>
                <th>{t('reports.table.entries')}</th>
                <th>{t('reports.table.totalHours')}</th>
              </tr>
            </thead>
            <tbody>
              {report.lines.map((line, idx) => (
                <tr key={`${line.userId}-${line.hourTypeId}-${line.periodLabel}-${idx}`}>
                  <td>{line.userFullName}</td>
                  <td>
                    <HourTypeBadge name={line.hourTypeName} colorHex={line.hourTypeColor} />
                  </td>
                  <td>{line.periodLabel}</td>
                  <td>
                    {line.periodStart} → {line.periodEnd}
                  </td>
                  <td>{line.entryCount}</td>
                  <td>{hoursToHm(line.totalHours)}</td>
                </tr>
              ))}
              {report.lines.length === 0 && (
                <tr>
                  <td colSpan={6} className="empty-state">
                    {t('reports.emptyState')}
                  </td>
                </tr>
              )}
            </tbody>
          </table>

          {balance.length > 0 && (
            <>
              <h2 className="reports__section-title">{t('reports.balanceTitle')}</h2>
              <p className="form__hint">{t('reports.balanceHint')}</p>
              <table className="list-view">
                <thead>
                  <tr>
                    <th>{t('reports.table.employee')}</th>
                    <th>{t('reports.table.period')}</th>
                    <th>{t('reports.table.range')}</th>
                    <th>{t('reports.table.worked')}</th>
                    <th>{t('reports.table.expected')}</th>
                    <th>{t('reports.table.difference')}</th>
                  </tr>
                </thead>
                <tbody>
                  {balance.map((line) => (
                    <tr key={`${line.userId}-${line.periodLabel}`}>
                      <td>{line.userFullName}</td>
                      <td>{line.periodLabel}</td>
                      <td>
                        {line.periodStart} → {line.periodEnd}
                      </td>
                      <td>{hoursToHm(line.workedHours)}</td>
                      <td>{hoursToHm(line.expectedHours)}</td>
                      <td className={line.differenceHours < 0 ? "balance-negative" : undefined}>
                        {signedHm(line.differenceHours)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </>
          )}
        </>
      )}

      {user?.role === "Employer" && <ClosedMonthsPanel />}
    </div>
  );
}

/** How many months back the panel offers, counting the current one. */
const MONTHS_SHOWN = 12;

/**
 * Employers close a month once its hours are final (e.g. after payroll). Nobody can then
 * change hours in it, employers included, until it's unlocked here.
 */
function ClosedMonthsPanel() {
  const { t, i18n } = useTranslation();
  const [locked, setLocked] = useState<Map<string, LockedMonth>>(new Map());
  const [busy, setBusy] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      const months = await getLockedMonths();
      setLocked(new Map(months.map((m) => [monthKey(m.year, m.month), m])));
    } catch (err) {
      setError(extractErrorMessage(err));
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const now = new Date();
  const months = Array.from({ length: MONTHS_SHOWN }, (_, i) => {
    const date = new Date(now.getFullYear(), now.getMonth() - i, 1);
    return { year: date.getFullYear(), month: date.getMonth() + 1, date };
  });
  const monthName = (date: Date) => date.toLocaleDateString(i18n.language, { month: "long", year: "numeric" });

  async function toggle(year: number, month: number, date: Date) {
    const key = monthKey(year, month);
    const isLocked = locked.has(key);
    if (!isLocked && !confirm(t('lockedMonths.confirmLock', { month: monthName(date) }))) return;

    setBusy(key);
    setError(null);
    try {
      if (isLocked) await unlockMonth(year, month);
      else await lockMonth(year, month);
      await load();
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setBusy(null);
    }
  }

  return (
    <div className="panel">
      <h2>{t('lockedMonths.title')}</h2>
      <p className="form__hint">{t('lockedMonths.hint')}</p>
      {error && <p className="form__error">{error}</p>}
      <table className="list-view">
        <tbody>
          {months.map(({ year, month, date }) => {
            const key = monthKey(year, month);
            const lock = locked.get(key);
            return (
              <tr key={key}>
                <td className="locked-months__name">{monthName(date)}</td>
                <td>
                  {lock ? (
                    <span className="locked-label">
                      🔒 {t('lockedMonths.lockedBy', {
                        name: lock.lockedByName,
                        date: formatDayLabel(lock.lockedAtUtc.slice(0, 10))
                      })}
                    </span>
                  ) : (
                    <span className="list-view__notes">{t('lockedMonths.open')}</span>
                  )}
                </td>
                <td className="list-view__actions">
                  <button
                    className={lock ? "btn btn--ghost btn--sm" : "btn btn--primary btn--sm"}
                    onClick={() => toggle(year, month, date)}
                    disabled={busy === key}
                  >
                    {lock ? t('lockedMonths.unlock') : t('lockedMonths.lock')}
                  </button>
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
