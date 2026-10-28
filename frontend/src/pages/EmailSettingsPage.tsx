import { FormEvent, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { extractErrorMessage } from "../api/client";
import { getMailSettings, sendTestEmail, updateMailSettings } from "../api/mailSettingsApi";
import type { MailProvider, MailSettings, SmtpSecurity } from "../types";
import { defaultSmtpPort, formFromSettings, MailSettingsForm, toUpdatePayload } from "../utils/mailSettings";

const checkboxFieldStyle = { flexDirection: "row", alignItems: "center", gap: "8px", cursor: "pointer" } as const;

/** The company's email settings. `embedded` renders just the panel, for the Company page. */
export function EmailSettingsPage({ embedded = false }: { embedded?: boolean } = {}) {
  const { t } = useTranslation();
  const [saved, setSaved] = useState<MailSettings | null>(null);
  const [form, setForm] = useState<MailSettingsForm | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [testing, setTesting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    getMailSettings()
      .then((settings) => {
        setSaved(settings);
        setForm(formFromSettings(settings));
      })
      .catch((err) => setError(extractErrorMessage(err)))
      .finally(() => setLoading(false));
  }, []);

  function update<K extends keyof MailSettingsForm>(key: K, value: MailSettingsForm[K]) {
    setForm((current) => (current ? { ...current, [key]: value } : current));
  }

  function changeSecurity(security: SmtpSecurity) {
    // Follow the port along only if it's still the default for the previous mode.
    setForm((current) =>
      current
        ? {
            ...current,
            smtpSecurity: security,
            smtpPort:
              current.smtpPort === defaultSmtpPort(current.smtpSecurity) ? defaultSmtpPort(security) : current.smtpPort
          }
        : current
    );
  }

  async function handleSave(e: FormEvent) {
    e.preventDefault();
    if (!form) return;
    setError(null);
    setSuccess(null);
    setSaving(true);
    try {
      const settings = await updateMailSettings(toUpdatePayload(form));
      setSaved(settings);
      setForm(formFromSettings(settings));
      setSuccess(t("emailSettings.saved"));
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  async function handleTest() {
    setError(null);
    setSuccess(null);
    setTesting(true);
    try {
      await sendTestEmail();
      setSuccess(t("emailSettings.testSent"));
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setTesting(false);
    }
  }

  if (loading) {
    return <p>{t("common.loading")}</p>;
  }

  if (!form) {
    return <p className="form__error">{error}</p>;
  }

  const isSmtp = form.provider === "Smtp";
  const secretPlaceholder = (isSet: boolean | undefined) => (isSet ? t("emailSettings.secretKept") : "");

  const content = (
      <form className="panel form" style={embedded ? undefined : { maxWidth: 640 }} onSubmit={handleSave}>
        {embedded && (
          <>
            <h2>{t("emailSettings.title")}</h2>
            <p className="form__hint">{t("emailSettings.subtitle")}</p>
          </>
        )}
        <label className="form__field" style={checkboxFieldStyle}>
          <input
            type="checkbox"
            checked={form.isEnabled}
            onChange={(e) => update("isEnabled", e.target.checked)}
            style={{ width: "auto" }}
          />
          <span>{t("emailSettings.enabled")}</span>
        </label>

        <label className="form__field">
          <span>{t("emailSettings.provider")}</span>
          <select value={form.provider} onChange={(e) => update("provider", e.target.value as MailProvider)}>
            <option value="Smtp">{t("emailSettings.providerSmtp")}</option>
            <option value="MicrosoftGraph">{t("emailSettings.providerGraph")}</option>
          </select>
        </label>

        <div className="form__row">
          <label className="form__field">
            <span>{t("emailSettings.fromAddress")}</span>
            <input
              type="email"
              value={form.fromAddress}
              onChange={(e) => update("fromAddress", e.target.value)}
              placeholder="noreply@company.com"
              required
            />
          </label>
          <label className="form__field">
            <span>{t("emailSettings.fromName")}</span>
            <input value={form.fromName} onChange={(e) => update("fromName", e.target.value)} />
          </label>
        </div>

        {isSmtp ? (
          <>
            <h2>{t("emailSettings.smtpTitle")}</h2>
            <div className="form__row">
              <label className="form__field">
                <span>{t("emailSettings.smtpHost")}</span>
                <input
                  value={form.smtpHost}
                  onChange={(e) => update("smtpHost", e.target.value)}
                  placeholder="smtp-relay.brevo.com"
                  required
                />
              </label>
              <label className="form__field">
                <span>{t("emailSettings.smtpSecurity")}</span>
                <select value={form.smtpSecurity} onChange={(e) => changeSecurity(e.target.value as SmtpSecurity)}>
                  <option value="StartTls">{t("emailSettings.securityStartTls")}</option>
                  <option value="SslOnConnect">{t("emailSettings.securitySsl")}</option>
                  <option value="None">{t("emailSettings.securityNone")}</option>
                </select>
              </label>
              <label className="form__field form__field--narrow">
                <span>{t("emailSettings.smtpPort")}</span>
                <input
                  type="number"
                  min={1}
                  max={65535}
                  value={form.smtpPort}
                  onChange={(e) => update("smtpPort", Number(e.target.value))}
                  required
                />
              </label>
            </div>
            <div className="form__row">
              <label className="form__field">
                <span>{t("emailSettings.smtpUsername")}</span>
                <input
                  value={form.smtpUsername}
                  onChange={(e) => update("smtpUsername", e.target.value)}
                  autoComplete="off"
                />
              </label>
              <label className="form__field">
                <span>{t("emailSettings.smtpPassword")}</span>
                <input
                  type="password"
                  value={form.smtpPassword}
                  onChange={(e) => update("smtpPassword", e.target.value)}
                  placeholder={secretPlaceholder(saved?.hasSmtpPassword)}
                  autoComplete="new-password"
                />
              </label>
            </div>
            <p className="page__subtitle">{t("emailSettings.smtpHint")}</p>
          </>
        ) : (
          <>
            <h2>{t("emailSettings.graphTitle")}</h2>
            <div className="form__row">
              <label className="form__field">
                <span>{t("emailSettings.graphTenantId")}</span>
                <input
                  value={form.graphTenantId}
                  onChange={(e) => update("graphTenantId", e.target.value)}
                  placeholder="contoso.onmicrosoft.com"
                  required
                />
              </label>
              <label className="form__field">
                <span>{t("emailSettings.graphClientId")}</span>
                <input value={form.graphClientId} onChange={(e) => update("graphClientId", e.target.value)} required />
              </label>
            </div>
            <div className="form__row">
              <label className="form__field">
                <span>{t("emailSettings.graphClientSecret")}</span>
                <input
                  type="password"
                  value={form.graphClientSecret}
                  onChange={(e) => update("graphClientSecret", e.target.value)}
                  placeholder={secretPlaceholder(saved?.hasGraphClientSecret)}
                  required={!saved?.hasGraphClientSecret}
                  autoComplete="new-password"
                />
              </label>
              <label className="form__field">
                <span>{t("emailSettings.graphMailbox")}</span>
                <input
                  type="email"
                  value={form.graphMailbox}
                  onChange={(e) => update("graphMailbox", e.target.value)}
                  placeholder="noreply@company.com"
                  required
                />
              </label>
            </div>
            <p className="page__subtitle">{t("emailSettings.graphHint")}</p>
          </>
        )}

        {error && <p className="form__error">{error}</p>}
        {success && <p className="form__success">{success}</p>}

        <div className="form__actions">
          <button className="btn btn--primary" type="submit" disabled={saving || testing}>
            {saving ? t("common.saving") : t("common.save")}
          </button>
          <button
            className="btn btn--ghost"
            type="button"
            onClick={handleTest}
            disabled={!saved?.isConfigured || saving || testing}
            title={saved?.isConfigured ? undefined : t("emailSettings.saveFirst")}
          >
            {testing ? t("emailSettings.testSending") : t("emailSettings.sendTest")}
          </button>
        </div>
      </form>
  );

  if (embedded) return content;

  return (
    <div className="page">
      <div className="page__header">
        <div>
          <h1>{t("emailSettings.title")}</h1>
          <p className="page__subtitle">{t("emailSettings.subtitle")}</p>
        </div>
      </div>
      {content}
    </div>
  );
}
