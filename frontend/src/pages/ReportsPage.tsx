import { useState } from "react";
import { format, startOfMonth } from "date-fns";
import { downloadHoursReportCsv, getHoursReport } from "../api/reportsApi";
import { extractErrorMessage } from "../api/client";
import type { HoursReport, ReportGrouping } from "../types";
import { hoursToHm } from "../utils/dateRange";

export function ReportsPage() {
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
          <h1>Reports</h1>
          <p className="page__subtitle">Extract worked hours by day, week, or month.</p>
        </div>
      </div>

      <div className="panel">
        <div className="form__row form__row--reports">
          <label className="form__field">
            <span>From</span>
            <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
          </label>
          <label className="form__field">
            <span>To</span>
            <input type="date" value={to} onChange={(e) => setTo(e.target.value)} />
          </label>
          <label className="form__field form__field--narrow">
            <span>Group by</span>
            <select value={grouping} onChange={(e) => setGrouping(e.target.value as ReportGrouping)}>
              <option value="Day">Day</option>
              <option value="Week">Week</option>
              <option value="Month">Month</option>
            </select>
          </label>
          <div className="form__actions form__actions--inline">
            <button className="btn btn--primary" onClick={runReport} disabled={loading}>
              {loading ? "Running…" : "Run report"}
            </button>
            {report && (
              <button className="btn btn--ghost" onClick={handleDownload}>
                Download CSV
              </button>
            )}
          </div>
        </div>
      </div>

      {error && <p className="form__error">{error}</p>}

      {report && (
        <>
          <p className="page__subtitle">
            Grand total: <strong>{hoursToHm(report.grandTotalHours)}</strong> across {report.lines.length} period
            {report.lines.length === 1 ? "" : "s"}
          </p>
          <table className="list-view">
            <thead>
              <tr>
                <th>Employee</th>
                <th>Period</th>
                <th>Range</th>
                <th>Entries</th>
                <th>Total hours</th>
              </tr>
            </thead>
            <tbody>
              {report.lines.map((line, idx) => (
                <tr key={`${line.userId}-${line.periodLabel}-${idx}`}>
                  <td>{line.userFullName}</td>
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
                  <td colSpan={5} className="empty-state">
                    No hours logged in this period.
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
