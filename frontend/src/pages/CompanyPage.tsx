import { FormEvent, useCallback, useEffect, useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { useAuth } from "../auth/AuthContext";
import { extractErrorMessage } from "../api/client";
import {
  addPublicHoliday,
  deletePublicHoliday,
  getCompany,
  getPublicHolidays,
  importPublicHolidays,
  updateCompany
} from "../api/companyApi";
import { WorkScheduleEditor } from "../components/WorkScheduleEditor";
import { EmailSettingsPage } from "./EmailSettingsPage";
import { formatDayLabel } from "../utils/dateRange";
import type { Company, PublicHoliday } from "../types";

/** Countries offered in the picker; any ISO code the holiday service knows works. */
const COUNTRY_CODES = [
  "BE", "NL", "LU", "FR", "DE", "AT", "CH", "GB", "IE", "ES", "PT", "IT",
  "DK", "SE", "NO", "FI", "PL", "CZ", "SK", "HU", "RO", "BG", "GR", "HR",
  "SI", "EE", "LV", "LT", "US", "CA", "AU", "NZ"
];

const emptyCompany: Company = { name: "", street: null, postalCode: null, city: null, countryCode: null, region: null };

/** Company-wide settings for employers: details, usual working hours, public holidays and email. */
export function CompanyPage() {
  const { t } = useTranslation();
  const [company, setCompany] = useState<Company | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getCompany()
      .then(setCompany)
      .catch((err) => setError(extractErrorMessage(err)));
  }, []);

  return (
    <div className="page">
      <div className="page__header">
        <div>
          <h1>{t('company.title')}</h1>
          <p className="page__subtitle">{t('company.subtitle')}</p>
        </div>
      </div>

      {error && <p className="form__error">{error}</p>}
      {company ? (
        <>
          <CompanyDetailsForm company={company} onSaved={setCompany} />
          <WorkScheduleEditor userId={null} />
          <HolidaysPanel company={company} />
          <EmailSettingsPage embedded />
        </>
      ) : (
        !error && <p className="empty-state">{t('common.loading')}</p>
      )}
    </div>
  );
}

function CompanyDetailsForm({ company, onSaved }: { company: Company; onSaved: (company: Company) => void }) {
  const { t, i18n } = useTranslation();
  const { updateTenantName } = useAuth();
  const [form, setForm] = useState<Company>({ ...emptyCompany, ...company });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);

  const countries = useMemo(() => {
    const names = new Intl.DisplayNames([i18n.language], { type: "region" });
    const codes = new Set(COUNTRY_CODES);
    if (company.countryCode) codes.add(company.countryCode);
    return [...codes]
      .map((code) => ({ code, name: names.of(code) ?? code }))
      .sort((a, b) => a.name.localeCompare(b.name, i18n.language));
  }, [i18n.language, company.countryCode]);

  function update<K extends keyof Company>(key: K, value: Company[K]) {
    setSaved(false);
    setForm((current) => ({ ...current, [key]: value }));
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSaving(true);
    setError(null);
    try {
      const updated = await updateCompany({
        name: form.name.trim(),
        street: form.street?.trim() || null,
        postalCode: form.postalCode?.trim() || null,
        city: form.city?.trim() || null,
        countryCode: form.countryCode || null,
        region: form.region?.trim().toUpperCase() || null
      });
      setForm(updated);
      onSaved(updated);
      updateTenantName(updated.name);
      setSaved(true);
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  return (
    <form className="panel form" onSubmit={handleSubmit}>
      <h2>{t('company.detailsTitle')}</h2>
      <div className="form__row">
        <label className="form__field">
          <span>{t('company.name')}</span>
          <input value={form.name} onChange={(e) => update("name", e.target.value)} maxLength={200} required />
        </label>
      </div>
      <div className="form__row">
        <label className="form__field">
          <span>{t('company.street')}</span>
          <input value={form.street ?? ""} onChange={(e) => update("street", e.target.value)} maxLength={200} />
        </label>
        <label className="form__field form__field--narrow">
          <span>{t('company.postalCode')}</span>
          <input value={form.postalCode ?? ""} onChange={(e) => update("postalCode", e.target.value)} maxLength={20} />
        </label>
        <label className="form__field">
          <span>{t('company.city')}</span>
          <input value={form.city ?? ""} onChange={(e) => update("city", e.target.value)} maxLength={100} />
        </label>
      </div>
      <div className="form__row">
        <label className="form__field">
          <span>{t('company.country')}</span>
          <select
            value={form.countryCode ?? ""}
            onChange={(e) => {
              update("countryCode", e.target.value || null);
              update("region", null);
            }}
          >
            <option value="">{t('company.noCountry')}</option>
            {countries.map((c) => (
              <option key={c.code} value={c.code}>
                {c.name}
              </option>
            ))}
          </select>
        </label>
        <label className="form__field">
          <span>{t('company.region')}</span>
          <input
            value={form.region ?? ""}
            onChange={(e) => update("region", e.target.value)}
            placeholder={form.countryCode ? `${form.countryCode}-…` : ""}
            disabled={!form.countryCode}
            maxLength={10}
          />
        </label>
      </div>
      <p className="form__hint">{t('company.regionHint')}</p>

      {error && <p className="form__error">{error}</p>}
      {saved && <p className="form__success">{t('company.saved')}</p>}
      <div className="form__actions">
        <button className="btn btn--primary" type="submit" disabled={saving}>
          {saving ? t('common.saving') : t('common.save')}
        </button>
      </div>
    </form>
  );
}

function HolidaysPanel({ company }: { company: Company }) {
  const { t, i18n } = useTranslation();
  const [year, setYear] = useState(new Date().getFullYear());
  const [holidays, setHolidays] = useState<PublicHoliday[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [newDate, setNewDate] = useState("");
  const [newName, setNewName] = useState("");

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setHolidays(await getPublicHolidays(year));
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }, [year]);

  useEffect(() => {
    setMessage(null);
    load();
  }, [load]);

  const countryName = company.countryCode
    ? new Intl.DisplayNames([i18n.language], { type: "region" }).of(company.countryCode) ?? company.countryCode
    : null;

  async function handleImport() {
    setBusy(true);
    setError(null);
    setMessage(null);
    try {
      const result = await importPublicHolidays(year);
      setHolidays(result.holidays);
      setMessage(result.added > 0 ? t('company.holidaysImported', { count: result.added }) : t('company.holidaysUpToDate'));
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  async function handleAdd(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    setMessage(null);
    try {
      await addPublicHoliday(newDate, newName.trim());
      setNewDate("");
      setNewName("");
      await load();
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  async function handleDelete(holiday: PublicHoliday) {
    if (!confirm(t('company.confirmDeleteHoliday', { name: holiday.name }))) return;
    setBusy(true);
    setError(null);
    try {
      await deletePublicHoliday(holiday.id);
      await load();
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="panel">
      <div className="page__header" style={{ marginBottom: '0.5rem' }}>
        <h2>{t('company.holidaysTitle', { year })}</h2>
        <div className="page__header-actions">
          <button className="btn btn--ghost btn--sm" onClick={() => setYear(year - 1)} aria-label={t('leave.prevYear')}>
            ←
          </button>
          <button className="btn btn--ghost btn--sm" onClick={() => setYear(year + 1)} aria-label={t('leave.nextYear')}>
            →
          </button>
        </div>
      </div>
      <p className="form__hint">{t('company.holidaysHint')}</p>

      <div className="form__actions" style={{ margin: '0.75rem 0' }}>
        <button
          className="btn btn--primary"
          onClick={handleImport}
          disabled={busy || !company.countryCode}
          title={company.countryCode ? undefined : t('company.setCountryFirst')}
        >
          {countryName
            ? t('company.importHolidays', { country: countryName, year })
            : t('company.setCountryFirst')}
        </button>
      </div>

      {error && <p className="form__error">{error}</p>}
      {message && <p className="form__success">{message}</p>}

      {loading ? (
        <p className="empty-state">{t('common.loading')}</p>
      ) : holidays.length === 0 ? (
        <p className="empty-state">{t('company.noHolidays')}</p>
      ) : (
        <table className="list-view">
          <thead>
            <tr>
              <th>{t('company.holidayDate')}</th>
              <th>{t('company.holidayName')}</th>
              <th aria-label={t('leave.table.actions')} />
            </tr>
          </thead>
          <tbody>
            {holidays.map((h) => (
              <tr key={h.id}>
                <td>{formatDayLabel(h.date)}</td>
                <td>{h.name}</td>
                <td className="list-view__actions">
                  <button className="btn btn--ghost btn--sm btn--danger" onClick={() => handleDelete(h)} disabled={busy}>
                    {t('common.delete')}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}

      <form className="form" onSubmit={handleAdd} style={{ marginTop: '1rem' }}>
        <div className="form__row">
          <label className="form__field">
            <span>{t('company.holidayDate')}</span>
            <input type="date" value={newDate} onChange={(e) => setNewDate(e.target.value)} required />
          </label>
          <label className="form__field">
            <span>{t('company.holidayName')}</span>
            <input
              value={newName}
              onChange={(e) => setNewName(e.target.value)}
              placeholder={t('company.holidayNamePlaceholder')}
              maxLength={200}
              required
            />
          </label>
          <div className="form__actions form__actions--inline">
            <button className="btn btn--ghost" type="submit" disabled={busy}>
              {t('company.addHoliday')}
            </button>
          </div>
        </div>
      </form>
    </div>
  );
}
