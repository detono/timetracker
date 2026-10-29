import { FormEvent, useCallback, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { HourTypeBadge } from "../components/HourTypeBadge";
import { useOutletContext } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { extractErrorMessage } from "../api/client";
import { getHourTypes } from "../api/hourTypesApi";
import {
  approveLeaveRequest,
  cancelLeaveRequest,
  createLeaveRequest,
  getLeaveBalances,
  getLeaveRequests,
  previewLeaveRequest,
  rejectLeaveRequest,
  setLeaveAllowance
} from "../api/leaveApi";
import { formatDayLabel, hoursToHm } from "../utils/dateRange";
import { localizedName, type ShellContext } from "../utils/leave";
import type { DayPart, HourType, LeaveBalance, LeavePreview, LeaveRequest } from "../types";
import type { TFunction } from "i18next";

function todayIso(): string {
  const now = new Date();
  const offset = now.getTimezoneOffset() * 60000;
  return new Date(now.getTime() - offset).toISOString().slice(0, 10);
}

/** "Wed 4 Nov (afternoon) – Fri 6 Nov", with the day parts only when they're half days. */
function period(
  request: Pick<LeaveRequest, "startDate" | "endDate" | "firstDayPart" | "lastDayPart">,
  t: TFunction
): string {
  const part = (p: DayPart) => (p === "Full" ? "" : ` (${t(`leave.part.${p}`)})`);
  return request.startDate === request.endDate
    ? formatDayLabel(request.startDate) + part(request.firstDayPart)
    : `${formatDayLabel(request.startDate)}${part(request.firstDayPart)} – ${formatDayLabel(request.endDate)}${part(request.lastDayPart)}`;
}

/** Hours that may be negative, e.g. a balance that ran out. */
function signedHours(hours: number): string {
  return hours < 0 ? `−${hoursToHm(-hours)}` : hoursToHm(hours);
}

function HourTypeLabel({ request }: { request: LeaveRequest }) {
  return <HourTypeBadge localizedNames={request.localizedHourTypeNames} colorHex={request.hourTypeColor} />;
}

function StatusLabel({ request }: { request: LeaveRequest }) {
  const { t } = useTranslation();
  return (
    <span className={`leave-status leave-status--${request.status.toLowerCase()}`}>
      {t(`leave.status.${request.status}`)}
    </span>
  );
}

export function LeavePage() {
  const { t } = useTranslation();
  const { user } = useAuth();
  const { refreshPendingLeaveCount } = useOutletContext<ShellContext>();
  const isEmployer = user?.role === "Employer";

  const [leaveTypes, setLeaveTypes] = useState<HourType[]>([]);
  const [mine, setMine] = useState<LeaveRequest[]>([]);
  const [toReview, setToReview] = useState<LeaveRequest[]>([]);
  const [showAllReviews, setShowAllReviews] = useState(false);
  const [year, setYear] = useState(new Date().getFullYear());
  const [balances, setBalances] = useState<LeaveBalance[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  const load = useCallback(async () => {
    setError(null);
    try {
      const [types, mineData, reviewData, balanceData] = await Promise.all([
        getHourTypes(),
        getLeaveRequests("Mine"),
        getLeaveRequests("ToReview", showAllReviews ? undefined : "Pending"),
        getLeaveBalances(year)
      ]);
      setLeaveTypes(types.filter((ht) => ht.isLeave));
      setMine(mineData);
      setToReview(reviewData);
      setBalances(balanceData);
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }, [showAllReviews, year]);

  useEffect(() => {
    load();
  }, [load]);

  async function afterChange() {
    await load();
    refreshPendingLeaveCount();
  }

  async function runAction(id: string, action: () => Promise<unknown>) {
    setBusyId(id);
    setError(null);
    try {
      await action();
      await afterChange();
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setBusyId(null);
    }
  }

  function handleApprove(request: LeaveRequest) {
    if (request.exceedsBalance) {
      const message = t('leave.confirmOverBalance', {
        name: request.userFullName,
        remaining: signedHours(request.remainingBalanceHours ?? 0),
        hours: hoursToHm(request.hours)
      });
      if (!confirm(message)) return;
    }
    runAction(request.id, () => approveLeaveRequest(request.id, null));
  }

  function handleReject(request: LeaveRequest) {
    const reason = prompt(t('leave.rejectReason', { name: request.userFullName }));
    if (reason === null) return;
    runAction(request.id, () => rejectLeaveRequest(request.id, reason.trim() || null));
  }

  function handleCancel(request: LeaveRequest) {
    if (request.status === "Approved") {
      const reason = prompt(t('leave.revokeReason', { name: request.userFullName }));
      if (reason === null) return;
      runAction(request.id, () => cancelLeaveRequest(request.id, reason.trim() || null));
    } else {
      if (!confirm(t('leave.confirmWithdraw'))) return;
      runAction(request.id, () => cancelLeaveRequest(request.id, null));
    }
  }

  const ownBalance = balances.find((b) => b.userId === user?.userId);
  const isReviewer = isEmployer || toReview.length > 0 || showAllReviews;

  return (
    <div className="page">
      <div className="page__header">
        <div>
          <h1>{t('leave.title')}</h1>
          <p className="page__subtitle">
            {ownBalance?.remainingHours != null && ownBalance.allowanceHours != null ? (
              <>
                {t('leave.myBalance', { year })}{" "}
                <strong className={ownBalance.remainingHours < 0 ? "balance-negative" : undefined}>
                  {t('leave.hoursLeftOf', {
                    left: signedHours(ownBalance.remainingHours),
                    total: hoursToHm(ownBalance.allowanceHours)
                  })}
                </strong>
                {ownBalance.pendingHours > 0 && ` · ${t('leave.pendingHours', { hours: hoursToHm(ownBalance.pendingHours) })}`}
              </>
            ) : (
              isEmployer ? t('leave.subtitleEmployer') : t('leave.subtitle')
            )}
          </p>
        </div>
      </div>

      {!isEmployer && <RequestLeaveForm leaveTypes={leaveTypes} onCreated={afterChange} />}

      {error && <p className="form__error">{error}</p>}

      {loading ? (
        <p className="empty-state">{t('common.loading')}</p>
      ) : (
        <>
          {isReviewer && (
            <div className="panel">
              <div className="page__header" style={{ marginBottom: '0.5rem' }}>
                <h2>{t('leave.toReviewTitle')}</h2>
                <label className="form__field" style={{ flexDirection: 'row', alignItems: 'center', gap: '8px' }}>
                  <input
                    type="checkbox"
                    checked={showAllReviews}
                    onChange={(e) => setShowAllReviews(e.target.checked)}
                    style={{ width: 'auto' }}
                  />
                  <span>{t('leave.showDecided')}</span>
                </label>
              </div>
              {toReview.length === 0 ? (
                <p className="empty-state">{t('leave.nothingToReview')}</p>
              ) : (
                <div className="table-scroll">
                  <table className="list-view">
                    <thead>
                      <tr>
                        <th>{t('leave.table.employee')}</th>
                        <th>{t('leave.table.type')}</th>
                        <th>{t('leave.table.period')}</th>
                        <th>{t('leave.table.hours')}</th>
                        <th>{t('leave.table.balance')}</th>
                        <th>{t('leave.table.status')}</th>
                        <th aria-label={t('leave.table.actions')} />
                      </tr>
                    </thead>
                    <tbody>
                      {toReview.map((r) => (
                        <tr key={r.id}>
                          <td>{r.userFullName}</td>
                          <td>
                            <HourTypeLabel request={r} />
                          </td>
                          <td>
                            {period(r, t)}
                            {r.comment && <div className="list-view__notes">{r.comment}</div>}
                          </td>
                          <td>{hoursToHm(r.hours)}</td>
                          <td>
                            {r.remainingBalanceHours != null ? (
                              <span className={r.exceedsBalance ? "balance-negative" : undefined}>
                                {t('leave.leftBefore', { hours: signedHours(r.remainingBalanceHours) })}
                                {r.exceedsBalance && ` · ${t('leave.overBalance')}`}
                              </span>
                            ) : (
                              <span className="list-view__notes">—</span>
                            )}
                          </td>
                          <td>
                            <StatusLabel request={r} />
                          </td>
                          <td className="list-view__actions">
                            {r.inLockedMonth && (
                              <span className="locked-label" title={t('lockedMonths.leaveLockedHint')}>
                                🔒 {t('lockedMonths.locked')}
                              </span>
                            )}
                            {r.status === "Pending" && r.canReview && (
                              <>
                                {!r.inLockedMonth && (
                                  <button
                                    className="btn btn--primary btn--sm"
                                    onClick={() => handleApprove(r)}
                                    disabled={busyId === r.id}
                                  >
                                    {t('leave.approve')}
                                  </button>
                                )}
                                <button
                                  className="btn btn--ghost btn--sm btn--danger"
                                  onClick={() => handleReject(r)}
                                  disabled={busyId === r.id}
                                >
                                  {t('leave.reject')}
                                </button>
                              </>
                            )}
                            {r.status === "Approved" && r.canCancel && (
                              <button
                                className="btn btn--ghost btn--sm btn--danger"
                                onClick={() => handleCancel(r)}
                                disabled={busyId === r.id}
                              >
                                {t('leave.revoke')}
                              </button>
                            )}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          )}

          {!isEmployer && (
          <div className="panel">
            <h2>{t('leave.myRequestsTitle')}</h2>
            {mine.length === 0 ? (
              <p className="empty-state">{t('leave.noRequests')}</p>
            ) : (
              <div className="table-scroll">
                <table className="list-view">
                  <thead>
                    <tr>
                      <th>{t('leave.table.type')}</th>
                      <th>{t('leave.table.period')}</th>
                      <th>{t('leave.table.hours')}</th>
                      <th>{t('leave.table.status')}</th>
                      <th>{t('leave.table.reviewedBy')}</th>
                      <th aria-label={t('leave.table.actions')} />
                    </tr>
                  </thead>
                  <tbody>
                    {mine.map((r) => (
                      <tr key={r.id}>
                        <td>
                          <HourTypeLabel request={r} />
                        </td>
                        <td>
                          {period(r, t)}
                          {r.comment && <div className="list-view__notes">{r.comment}</div>}
                        </td>
                        <td>{hoursToHm(r.hours)}</td>
                        <td>
                          <StatusLabel request={r} />
                          {r.exceedsBalance && <div className="balance-negative">{t('leave.overBalance')}</div>}
                        </td>
                        <td>
                          {r.reviewedByName ?? <span className="list-view__notes">—</span>}
                          {r.reviewComment && <div className="list-view__notes">“{r.reviewComment}”</div>}
                        </td>
                        <td className="list-view__actions">
                          {r.canCancel && (
                            <button
                              className="btn btn--ghost btn--sm btn--danger"
                              onClick={() => handleCancel(r)}
                              disabled={busyId === r.id}
                            >
                              {r.status === "Approved" ? t('leave.revoke') : t('leave.withdraw')}
                            </button>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
          )}

          {(isEmployer || balances.length > 1) && (
            <BalancesPanel
              year={year}
              onYearChange={setYear}
              balances={balances}
              canEdit={isEmployer}
              onSaved={load}
              onError={setError}
            />
          )}
        </>
      )}
    </div>
  );
}

function RequestLeaveForm({ leaveTypes, onCreated }: { leaveTypes: HourType[]; onCreated: () => Promise<void> }) {
  const { t, i18n } = useTranslation();
  const [hourTypeId, setHourTypeId] = useState("");
  const [startDate, setStartDate] = useState(todayIso());
  const [endDate, setEndDate] = useState(todayIso());
  const [comment, setComment] = useState("");
  // For a single day, firstDayPart is the part of that day.
  const [firstDayPart, setFirstDayPart] = useState<DayPart>("Full");
  const [lastDayPart, setLastDayPart] = useState<DayPart>("Full");
  const singleDay = startDate === endDate;
  const effectiveLastPart = singleDay ? firstDayPart : lastDayPart;
  const [preview, setPreview] = useState<LeavePreview | null>(null);
  const [previewError, setPreviewError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [previewVersion, setPreviewVersion] = useState(0);

  // Default to the first leave type once they're loaded.
  useEffect(() => {
    if (!leaveTypes.some((lt) => lt.id === hourTypeId)) {
      setHourTypeId(leaveTypes[0]?.id ?? "");
    }
  }, [leaveTypes, hourTypeId]);

  // Over several days only the first day can be an afternoon and the last a morning.
  useEffect(() => {
    if (singleDay) return;
    if (firstDayPart === "Morning") setFirstDayPart("Full");
    if (lastDayPart === "Afternoon") setLastDayPart("Full");
  }, [singleDay, firstDayPart, lastDayPart]);

  // Live preview of hours and balance, debounced while the user is picking dates.
  useEffect(() => {
    setPreview(null);
    setPreviewError(null);
    if (!hourTypeId || !startDate || !endDate || endDate < startDate) return;

    let cancelled = false;
    const timer = setTimeout(() => {
      previewLeaveRequest(hourTypeId, startDate, endDate, firstDayPart, effectiveLastPart)
        .then((p) => !cancelled && setPreview(p))
        .catch((err) => !cancelled && setPreviewError(extractErrorMessage(err)));
    }, 250);
    return () => {
      cancelled = true;
      clearTimeout(timer);
    };
  }, [hourTypeId, startDate, endDate, firstDayPart, effectiveLastPart, previewVersion]);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      await createLeaveRequest({
        hourTypeId,
        startDate,
        endDate,
        comment: comment.trim() || null,
        firstDayPart,
        lastDayPart: effectiveLastPart
      });
      setComment("");
      setPreviewVersion((v) => v + 1);
      await onCreated();
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setSubmitting(false);
    }
  }

  if (leaveTypes.length === 0) {
    return (
      <div className="panel">
        <h2>{t('leave.requestTitle')}</h2>
        <p className="form__hint">{t('leave.noLeaveTypes')}</p>
      </div>
    );
  }

  const blocked = !preview || preview.workingDays === 0 || preview.overlapsExistingRequest;

  return (
    <div className="panel">
      <h2>{t('leave.requestTitle')}</h2>
      <form className="form" onSubmit={handleSubmit}>
        <div className="form__row">
          <label className="form__field">
            <span>{t('leave.type')}</span>
            <select value={hourTypeId} onChange={(e) => setHourTypeId(e.target.value)} required>
              {leaveTypes.map((lt) => (
                <option key={lt.id} value={lt.id}>
                  {localizedName(lt.localizedNames, i18n.language)}
                </option>
              ))}
            </select>
          </label>
          <label className="form__field">
            <span>{t('leave.from')}</span>
            <input
              type="date"
              value={startDate}
              onChange={(e) => {
                setStartDate(e.target.value);
                if (e.target.value > endDate) setEndDate(e.target.value);
              }}
              required
            />
          </label>
          <label className="form__field">
            <span>{t('leave.to')}</span>
            <input type="date" value={endDate} min={startDate} onChange={(e) => setEndDate(e.target.value)} required />
          </label>
        </div>
        <div className="form__row">
          {singleDay ? (
            <label className="form__field">
              <span>{t('leave.dayPart')}</span>
              <select value={firstDayPart} onChange={(e) => setFirstDayPart(e.target.value as DayPart)}>
                <option value="Full">{t('leave.wholeDay')}</option>
                <option value="Morning">{t('leave.morningOnly')}</option>
                <option value="Afternoon">{t('leave.afternoonOnly')}</option>
              </select>
            </label>
          ) : (
            <>
              <label className="form__field">
                <span>{t('leave.firstDay')}</span>
                <select value={firstDayPart} onChange={(e) => setFirstDayPart(e.target.value as DayPart)}>
                  <option value="Full">{t('leave.wholeDay')}</option>
                  <option value="Afternoon">{t('leave.fromAfternoon')}</option>
                </select>
              </label>
              <label className="form__field">
                <span>{t('leave.lastDay')}</span>
                <select value={lastDayPart} onChange={(e) => setLastDayPart(e.target.value as DayPart)}>
                  <option value="Full">{t('leave.wholeDay')}</option>
                  <option value="Morning">{t('leave.untilNoon')}</option>
                </select>
              </label>
            </>
          )}
        </div>
        <label className="form__field">
          <span>{t('leave.comment')}</span>
          <input value={comment} onChange={(e) => setComment(e.target.value)} maxLength={1000} />
        </label>

        {previewError && <p className="form__error">{previewError}</p>}
        {preview &&
          (preview.workingDays === 0 ? (
            <p className="form__warning">{t('leave.noWorkingDays')}</p>
          ) : (
            <>
              <p className="form__hint">
                {t('leave.previewDays', { count: preview.workingDays })} · <strong>{hoursToHm(preview.hours)}</strong>
                {preview.remainingBalanceHours != null &&
                  ` · ${t('leave.leftBefore', { hours: signedHours(preview.remainingBalanceHours) })}`}
              </p>
              {preview.exceedsBalance && <p className="form__warning">{t('leave.previewOverBalance')}</p>}
              {preview.overlapsExistingRequest && <p className="form__warning">{t('leave.overlaps')}</p>}
            </>
          ))}
        {error && <p className="form__error">{error}</p>}

        <div className="form__actions">
          <button className="btn btn--primary" type="submit" disabled={submitting || blocked}>
            {submitting ? t('common.saving') : t('leave.submit')}
          </button>
        </div>
      </form>
    </div>
  );
}

interface BalancesPanelProps {
  year: number;
  onYearChange: (year: number) => void;
  balances: LeaveBalance[];
  canEdit: boolean;
  onSaved: () => Promise<void>;
  onError: (message: string) => void;
}

function BalancesPanel({ year, onYearChange, balances, canEdit, onSaved, onError }: BalancesPanelProps) {
  const { t } = useTranslation();
  const [drafts, setDrafts] = useState<Record<string, string>>({});
  const [savingId, setSavingId] = useState<string | null>(null);

  useEffect(() => setDrafts({}), [year]);

  async function save(balance: LeaveBalance) {
    const value = Number((drafts[balance.userId] ?? "").replace(",", "."));
    if (!Number.isFinite(value) || value < 0) return;
    setSavingId(balance.userId);
    try {
      await setLeaveAllowance(balance.userId, year, value);
      setDrafts((current) => {
        const next = { ...current };
        delete next[balance.userId];
        return next;
      });
      await onSaved();
    } catch (err) {
      onError(extractErrorMessage(err));
    } finally {
      setSavingId(null);
    }
  }

  return (
    <div className="panel">
      <div className="page__header" style={{ marginBottom: '0.5rem' }}>
        <h2>{t('leave.balancesTitle', { year })}</h2>
        <div className="page__header-actions">
          <button className="btn btn--ghost btn--sm" onClick={() => onYearChange(year - 1)} aria-label={t('leave.prevYear')}>
            ←
          </button>
          <button className="btn btn--ghost btn--sm" onClick={() => onYearChange(year + 1)} aria-label={t('leave.nextYear')}>
            →
          </button>
        </div>
      </div>
      {canEdit && <p className="form__hint">{t('leave.balancesHint')}</p>}
      <div className="table-scroll">
        <table className="list-view">
          <thead>
            <tr>
              <th>{t('leave.table.employee')}</th>
              <th>{t('leave.table.allowance')}</th>
              <th>{t('leave.table.used')}</th>
              <th>{t('leave.table.pending')}</th>
              <th>{t('leave.table.remaining')}</th>
            </tr>
          </thead>
          <tbody>
            {balances.map((b) => {
              const draft = drafts[b.userId];
              return (
                <tr key={b.userId}>
                  <td>{b.userFullName}</td>
                  <td>
                    {canEdit ? (
                      <form
                        style={{ display: 'flex', gap: '0.4rem', alignItems: 'center' }}
                        onSubmit={(e) => {
                          e.preventDefault();
                          save(b);
                        }}
                      >
                        <input
                          type="number"
                          min={0}
                          step={0.5}
                          style={{ width: '6rem' }}
                          aria-label={t('leave.allowanceFor', { name: b.userFullName })}
                          placeholder={t('leave.notSet')}
                          value={draft ?? (b.allowanceHours ?? "").toString()}
                          onChange={(e) => setDrafts({ ...drafts, [b.userId]: e.target.value })}
                        />
                        {draft !== undefined && draft !== "" && (
                          <button className="btn btn--primary btn--sm" type="submit" disabled={savingId === b.userId}>
                            {t('common.save')}
                          </button>
                        )}
                      </form>
                    ) : b.allowanceHours != null ? (
                      hoursToHm(b.allowanceHours)
                    ) : (
                      <span className="list-view__notes">{t('leave.notSet')}</span>
                    )}
                  </td>
                  <td>{hoursToHm(b.usedHours)}</td>
                  <td>{b.pendingHours > 0 ? hoursToHm(b.pendingHours) : <span className="list-view__notes">—</span>}</td>
                  <td>
                    {b.remainingHours != null ? (
                      <span className={b.remainingHours < 0 ? "balance-negative" : undefined}>
                        {signedHours(b.remainingHours)}
                        {b.remainingHours <= 0 && ` · ${t('leave.usedUp')}`}
                      </span>
                    ) : (
                      <span className="list-view__notes">—</span>
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}
