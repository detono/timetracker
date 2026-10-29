import { useCallback, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { getAuditLog } from "../api/auditLogApi";
import { extractErrorMessage } from "../api/client";
import type { AuditEntry } from "../types";
import { dateLocale } from "../utils/dateRange";

const PAGE_SIZE = 50;

/** The company's activity log for employers: who locked months, changed others' hours, reset passwords. */
export function ActivityLogPanel() {
  const { t, i18n } = useTranslation();
  const [entries, setEntries] = useState<AuditEntry[]>([]);
  const [hasMore, setHasMore] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async (before?: string) => {
    setLoading(true);
    setError(null);
    try {
      const page = await getAuditLog(before, PAGE_SIZE);
      setEntries((current) => (before ? [...current, ...page] : page));
      setHasMore(page.length === PAGE_SIZE);
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  function describe(entry: AuditEntry): string {
    const actor =
      entry.actorRole === "PlatformAdmin"
        ? t("activityLog.platformAdmin", { name: entry.actorName })
        : entry.actorName;
    const month = entry.details.month
      ? new Date(`${entry.details.month}-01T00:00:00`).toLocaleDateString(dateLocale(i18n.language), {
          month: "long",
          year: "numeric"
        })
      : undefined;
    return t(`activityLog.actions.${entry.action}`, {
      ...entry.details,
      actor,
      subject: entry.subjectName ?? "—",
      month,
      defaultValue: `${actor}: ${entry.action}`
    });
  }

  const when = (iso: string) =>
    new Date(iso).toLocaleString(dateLocale(i18n.language), { dateStyle: "medium", timeStyle: "short" });

  return (
    <div className="panel">
      <h2>{t("activityLog.title")}</h2>
      <p className="form__hint">{t("activityLog.hint")}</p>
      {error && <p className="form__error">{error}</p>}

      {entries.length === 0 && !loading ? (
        <p className="empty-state">{t("activityLog.empty")}</p>
      ) : (
        <ul className="activity-log">
          {entries.map((entry) => (
            <li key={entry.id} className="activity-log__item">
              <time className="activity-log__time" dateTime={entry.atUtc}>
                {when(entry.atUtc)}
              </time>
              <span>{describe(entry)}</span>
            </li>
          ))}
        </ul>
      )}

      {loading && <p className="empty-state">{t("common.loading")}</p>}
      {hasMore && !loading && (
        <button className="btn btn--ghost btn--sm" onClick={() => load(entries[entries.length - 1].atUtc)}>
          {t("activityLog.loadMore")}
        </button>
      )}
    </div>
  );
}
