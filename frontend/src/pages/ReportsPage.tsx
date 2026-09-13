import { useState } from "react";
import { useTranslation } from "react-i18next";
import { format, startOfMonth } from "date-fns";
import { downloadHoursReportCsv, getHoursReport } from "../api/reportsApi";
import { extractErrorMessage } from "../api/client";
import { HourTypeBadge } from "../components/HourTypeBadge";
import type { HoursReport, ReportGrouping } from "../types";
import { hoursToHm } from "../utils/dateRange";

export function ReportsPage() {
  const { t } = useTranslation();
  const [from, setFrom] = useState(format(startOfMonth(new Date()), "yyyy-MM-dd"));
  const [to, setTo] = useState(format(new Date(), "yyyy-MM-dd"));
  const [grouping, setGrouping] = useState<ReportGrouping>("Week");
  const [report, setReport] = useState<HoursReport | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function runReport() {
    setLoading(true);
    setError(null);
    try {
      const data = await getHoursReport(from, to, grouping);
      setReport(data);
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }

  async function handleDownload() {
    try {
      await downloadHoursReportCsv(from, to, grouping);
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
              <button className="btn btn--ghost" onClick={handleDownload}>
                {t('reports.downloadCsv')}
              </button>
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
                    <HourTypeBadge
                      localizedNames={line.localizedHourTypeNames}
                      colorHex={line.hourTypeColor}
                    />
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
        </>
      )}
    </div>
  );
}