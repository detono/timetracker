import { FormEvent, useCallback, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import type { TFunction } from "i18next";
import { extractErrorMessage } from "../api/client";
import {
  createWebhook,
  deleteWebhook,
  getWebhooks,
  getWebhookSecret,
  retryWebhookDelivery,
  rotateWebhookSecret,
  testWebhook,
  updateWebhook,
  type WebhookPayload
} from "../api/webhooksApi";
import { useConfirm } from "./ConfirmDialog";
import { RowMenu } from "./RowMenu";
import { dateLocale } from "../utils/dateRange";
import type { Webhook, WebhookDelivery, WebhooksOverview } from "../types";

const checkboxFieldStyle = { flexDirection: "row", alignItems: "center", gap: "8px", cursor: "pointer" } as const;

/** "leave.approved" → "Leave approved", in the user's language; unknown events show as they are. */
function eventLabel(event: string, t: TFunction): string {
  return t(`webhooks.events.${event.replace(/\./g, "_")}`, { defaultValue: event });
}

function formatTime(iso: string): string {
  return new Date(iso).toLocaleString(dateLocale(), { dateStyle: "medium", timeStyle: "short" });
}

interface Draft {
  name: string;
  url: string;
  events: string[];
  isEnabled: boolean;
  headerName: string;
  headerValue: string;
}

const emptyDraft: Draft = { name: "", url: "https://", events: [], isEnabled: true, headerName: "", headerValue: "" };

function draftOf(webhook: Webhook): Draft {
  return {
    name: webhook.name,
    url: webhook.url,
    events: webhook.events,
    isEnabled: webhook.isEnabled,
    headerName: webhook.headerName ?? "",
    headerValue: ""
  };
}

/**
 * Company → Webhooks: addresses this app calls when something happens, like leave being
 * approved, with the latest calls and how they went.
 */
export function WebhooksPanel() {
  const { t } = useTranslation();
  const { confirm } = useConfirm();
  const [overview, setOverview] = useState<WebhooksOverview | null>(null);
  const [error, setError] = useState<string | null>(null);
  // null: no form open; "new": adding one; otherwise the webhook being edited.
  const [editing, setEditing] = useState<Webhook | "new" | null>(null);
  // Shown in the editor after it switches from adding to editing the new webhook.
  const [editorNotice, setEditorNotice] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      setOverview(await getWebhooks());
      setError(null);
    } catch (err) {
      setError(extractErrorMessage(err));
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  async function handleDelete(webhook: Webhook) {
    const confirmed = await confirm({
      title: t("webhooks.deleteTitle", { name: webhook.name }),
      message: t("webhooks.deleteMessage"),
      confirmLabel: t("webhooks.delete"),
      tone: "danger"
    });
    if (!confirmed) return;
    try {
      await deleteWebhook(webhook.id);
      if (editing !== "new" && editing?.id === webhook.id) setEditing(null);
      await load();
    } catch (err) {
      setError(extractErrorMessage(err));
    }
  }

  async function handleRetry(delivery: WebhookDelivery) {
    try {
      await retryWebhookDelivery(delivery.id);
      await load();
    } catch (err) {
      setError(extractErrorMessage(err));
    }
  }

  if (!overview) {
    return error ? <p className="form__error">{error}</p> : <p className="empty-state">{t("common.loading")}</p>;
  }

  return (
    <div className="panel">
      <div className="page__header" style={{ marginBottom: "0.5rem" }}>
        <div>
          <h2>{t("webhooks.title")}</h2>
          <p className="form__hint">{t("webhooks.subtitle")}</p>
        </div>
        {editing === null && (
          <button
            className="btn btn--primary"
            onClick={() => {
              setEditorNotice(null);
              setEditing("new");
            }}
          >
            {t("webhooks.add")}
          </button>
        )}
      </div>

      {error && <p className="form__error">{error}</p>}

      {editing !== null && (
        <WebhookEditor
          key={editing === "new" ? "new" : editing.id}
          webhook={editing === "new" ? null : editing}
          availableEvents={overview.availableEvents}
          notice={editorNotice}
          onSaved={async (saved, created) => {
            await load();
            // A new webhook stays open, so its secret can be copied right away.
            setEditorNotice(created ? t("webhooks.created") : null);
            setEditing(saved);
          }}
          onTested={load}
          onClose={() => {
            setEditing(null);
            setEditorNotice(null);
          }}
        />
      )}

      {overview.webhooks.length === 0 ? (
        editing === null && <p className="empty-state">{t("webhooks.none")}</p>
      ) : (
        <div className="table-scroll table-scroll--stack">
          <table className="list-view">
            <thead>
              <tr>
                <th>{t("webhooks.table.webhook")}</th>
                <th>{t("webhooks.table.events")}</th>
                <th>{t("webhooks.table.status")}</th>
                <th aria-label={t("common.actions")} />
              </tr>
            </thead>
            <tbody>
              {overview.webhooks.map((w) => (
                <tr key={w.id}>
                  <td className="list-view__wrap">
                    <div className="person-cell">
                      <span className="person-cell__name">{w.name}</span>
                      <span className="person-cell__email mono">{w.url}</span>
                    </div>
                  </td>
                  <td className="list-view__wrap" data-label={t("webhooks.table.events")}>
                    {w.events.map((e) => eventLabel(e, t)).join(" · ")}
                  </td>
                  <td>
                    <span className={w.isEnabled ? "status status--active" : "status status--inactive"}>
                      {w.isEnabled ? t("webhooks.on") : t("webhooks.off")}
                    </span>
                  </td>
                  <td className="list-view__actions">
                    <RowMenu
                      label={`${t("common.actions")}: ${w.name}`}
                      items={[
                        {
                          label: t("webhooks.edit"),
                          onSelect: () => {
                            setEditorNotice(null);
                            setEditing(w);
                          }
                        },
                        { label: t("webhooks.delete"), onSelect: () => handleDelete(w), danger: true }
                      ]}
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {overview.webhooks.length > 0 && (
        <>
          <div className="page__header" style={{ margin: "1.5rem 0 0.5rem" }}>
            <h3>{t("webhooks.recentTitle")}</h3>
            <button className="btn btn--ghost btn--sm" onClick={load}>
              {t("webhooks.refresh")}
            </button>
          </div>
          {overview.recentDeliveries.length === 0 ? (
            <p className="empty-state">{t("webhooks.noCalls")}</p>
          ) : (
            <div className="table-scroll table-scroll--stack">
              <table className="list-view">
                <thead>
                  <tr>
                    <th>{t("webhooks.table.when")}</th>
                    <th>{t("webhooks.table.event")}</th>
                    <th>{t("webhooks.table.result")}</th>
                    <th aria-label={t("common.actions")} />
                  </tr>
                </thead>
                <tbody>
                  {overview.recentDeliveries.map((d) => (
                    <DeliveryRow key={d.id} delivery={d} onRetry={() => handleRetry(d)} />
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </>
      )}
    </div>
  );
}

function DeliveryRow({ delivery: d, onRetry }: { delivery: WebhookDelivery; onRetry: () => void }) {
  const { t } = useTranslation();
  const statusClass =
    d.status === "Succeeded" ? "leave-status--approved" : d.status === "Failed" ? "leave-status--rejected" : "leave-status--pending";
  const statusText =
    d.status === "Succeeded"
      ? t("webhooks.delivered", { code: d.lastStatusCode ?? 200 })
      : d.status === "Failed"
        ? t("webhooks.failed")
        : d.attempts === 0
          ? t("webhooks.waiting")
          : t("webhooks.retrying");

  return (
    <tr>
      <td data-label={t("webhooks.table.when")}>{formatTime(d.createdAtUtc)}</td>
      <td className="list-view__wrap">
        {d.eventType === "webhook.test" ? t("webhooks.testEvent") : eventLabel(d.eventType, t)}
        <div className="list-view__notes">
          {d.webhookName}
          {d.eventType !== "webhook.test" && ` · ${d.summary}`}
        </div>
      </td>
      <td className="list-view__wrap">
        <span className={`leave-status ${statusClass}`}>{statusText}</span>
        {d.status !== "Succeeded" && d.lastError && <div className="list-view__notes">{d.lastError}</div>}
        {d.status === "Pending" && d.attempts > 0 && d.nextAttemptAtUtc && (
          <div className="list-view__notes">
            {t("webhooks.nextTry", { count: d.attempts, time: formatTime(d.nextAttemptAtUtc) })}
          </div>
        )}
      </td>
      <td className="list-view__actions">
        {d.status === "Failed" && d.eventType !== "webhook.test" && (
          <button className="btn btn--ghost btn--sm" onClick={onRetry}>
            {t("webhooks.sendAgain")}
          </button>
        )}
      </td>
    </tr>
  );
}

interface EditorProps {
  webhook: Webhook | null;
  availableEvents: string[];
  /** A message to start with, e.g. "Webhook added" right after creating it. */
  notice: string | null;
  onSaved: (webhook: Webhook, created: boolean) => void | Promise<void>;
  onTested: () => void;
  onClose: () => void;
}

function WebhookEditor({ webhook, availableEvents, notice, onSaved, onTested, onClose }: EditorProps) {
  const { t } = useTranslation();
  const [draft, setDraft] = useState<Draft>(webhook ? draftOf(webhook) : emptyDraft);
  const [saving, setSaving] = useState(false);
  const [testing, setTesting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(notice);

  function toggleEvent(event: string, on: boolean) {
    setDraft((d) => ({ ...d, events: on ? [...d.events, event] : d.events.filter((e) => e !== event) }));
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (draft.events.length === 0) {
      setError(t("webhooks.chooseEvent"));
      return;
    }
    setSaving(true);
    setError(null);
    setSuccess(null);
    const payload: WebhookPayload = {
      name: draft.name,
      url: draft.url.trim(),
      events: availableEvents.filter((ev) => draft.events.includes(ev)),
      isEnabled: draft.isEnabled,
      headerName: draft.headerName.trim() || null,
      headerValue: draft.headerValue || null
    };
    try {
      const saved = webhook ? await updateWebhook(webhook.id, payload) : await createWebhook(payload);
      setDraft((d) => ({ ...d, headerValue: "" }));
      setSuccess(t("webhooks.saved"));
      await onSaved(saved, !webhook);
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  async function handleTest() {
    if (!webhook) return;
    setTesting(true);
    setError(null);
    setSuccess(null);
    try {
      const result = await testWebhook(webhook.id);
      if (result.status === "Succeeded") {
        setSuccess(t("webhooks.testOk", { code: result.lastStatusCode ?? 200 }));
      } else {
        setError(t("webhooks.testFailed", { error: result.lastError ?? "" }));
      }
      onTested();
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setTesting(false);
    }
  }

  const keepsHeaderValue = webhook?.hasHeaderValue && draft.headerName.trim().toLowerCase() === (webhook.headerName ?? "").toLowerCase();

  return (
    <form className="form webhook-editor" onSubmit={handleSubmit}>
      <h3>{webhook ? t("webhooks.editTitle", { name: webhook.name }) : t("webhooks.addTitle")}</h3>

      <div className="form__row">
        <label className="form__field">
          <span>{t("webhooks.name")}</span>
          <input
            value={draft.name}
            onChange={(e) => setDraft({ ...draft, name: e.target.value })}
            placeholder={t("webhooks.namePlaceholder")}
            maxLength={100}
            required
          />
        </label>
        <label className="form__field">
          <span>{t("webhooks.url")}</span>
          <input
            className="mono"
            type="url"
            value={draft.url}
            onChange={(e) => setDraft({ ...draft, url: e.target.value })}
            placeholder="https://hooks.example.com/leave"
            maxLength={2048}
            required
          />
        </label>
      </div>

      <fieldset className="form__field webhook-editor__events">
        <legend>{t("webhooks.sendWhen")}</legend>
        <div className="webhook-editor__checks">
          {availableEvents.map((ev) => (
            <label key={ev} className="form__field" style={checkboxFieldStyle}>
              <input
                type="checkbox"
                checked={draft.events.includes(ev)}
                onChange={(e) => toggleEvent(ev, e.target.checked)}
                style={{ width: "auto" }}
              />
              <span>{eventLabel(ev, t)}</span>
            </label>
          ))}
        </div>
      </fieldset>

      <div className="form__row">
        <label className="form__field">
          <span>{t("webhooks.headerName")}</span>
          <input
            className="mono"
            value={draft.headerName}
            onChange={(e) => setDraft({ ...draft, headerName: e.target.value })}
            placeholder="Authorization"
            maxLength={100}
          />
        </label>
        <label className="form__field">
          <span>{t("webhooks.headerValue")}</span>
          <input
            className="mono"
            type="password"
            autoComplete="off"
            value={draft.headerValue}
            onChange={(e) => setDraft({ ...draft, headerValue: e.target.value })}
            placeholder={keepsHeaderValue ? t("webhooks.headerValueKept") : "Bearer …"}
            disabled={!draft.headerName.trim()}
          />
        </label>
      </div>
      <p className="form__hint">{t("webhooks.headerHint")}</p>

      {webhook && <SecretField webhookId={webhook.id} />}

      <label className="form__field" style={checkboxFieldStyle}>
        <input
          type="checkbox"
          checked={draft.isEnabled}
          onChange={(e) => setDraft({ ...draft, isEnabled: e.target.checked })}
          style={{ width: "auto" }}
        />
        <span>{t("webhooks.enabled")}</span>
      </label>

      {error && <p className="form__error">{error}</p>}
      {success && <p className="form__success">{success}</p>}

      <div className="form__actions">
        <button className="btn btn--primary" type="submit" disabled={saving || testing}>
          {saving ? t("common.saving") : t("common.save")}
        </button>
        {webhook && (
          <button className="btn btn--ghost" type="button" onClick={handleTest} disabled={saving || testing}>
            {testing ? t("webhooks.testing") : t("webhooks.sendTest")}
          </button>
        )}
        <button className="btn btn--ghost" type="button" onClick={onClose}>
          {t("common.close")}
        </button>
      </div>
    </form>
  );
}

/** The signing secret: hidden until asked for, with copy and replace. */
function SecretField({ webhookId }: { webhookId: string }) {
  const { t } = useTranslation();
  const { confirm } = useConfirm();
  const [secret, setSecret] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function run(action: () => Promise<string>) {
    setError(null);
    setCopied(false);
    try {
      setSecret(await action());
    } catch (err) {
      setError(extractErrorMessage(err));
    }
  }

  async function handleRotate() {
    const confirmed = await confirm({
      title: t("webhooks.rotateTitle"),
      message: t("webhooks.rotateMessage"),
      confirmLabel: t("webhooks.rotate"),
      tone: "danger"
    });
    if (confirmed) await run(() => rotateWebhookSecret(webhookId));
  }

  async function handleCopy() {
    if (!secret) return;
    try {
      await navigator.clipboard.writeText(secret);
      setCopied(true);
    } catch {
      // No clipboard access (e.g. plain http): the secret is on screen to copy by hand.
    }
  }

  return (
    <div className="form__field">
      <span>{t("webhooks.secret")}</span>
      <div className="webhook-editor__secret">
        <code className="mono">{secret ?? "whsec_••••••••••••"}</code>
        {secret ? (
          <button className="btn btn--ghost btn--sm" type="button" onClick={handleCopy}>
            {copied ? t("webhooks.copied") : t("webhooks.copy")}
          </button>
        ) : (
          <button className="btn btn--ghost btn--sm" type="button" onClick={() => run(() => getWebhookSecret(webhookId))}>
            {t("webhooks.show")}
          </button>
        )}
        <button className="btn btn--ghost btn--sm" type="button" onClick={handleRotate}>
          {t("webhooks.rotate")}
        </button>
      </div>
      <p className="form__hint">{t("webhooks.secretHint")}</p>
      {error && <p className="form__error">{error}</p>}
    </div>
  );
}
