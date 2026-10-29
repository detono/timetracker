import { FormEvent, useCallback, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import {
  createTenant,
  getTenantEmployers,
  getTenants,
  resetEmployerPassword,
  setTenantActive,
  type CreateTenantPayload
} from "../api/tenantsApi";
import { extractErrorMessage } from "../api/client";
import type { Tenant, TenantEmployer } from "../types";
import { RowMenu } from "../components/RowMenu";

const emptyForm: CreateTenantPayload = {
  name: "",
  employerFirstName: "",
  employerLastName: "",
  employerEmail: "",
  employerPassword: ""
};

export function TenantsPage() {
  const { t, i18n } = useTranslation();
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [creating, setCreating] = useState(false);
  const [busyTenantId, setBusyTenantId] = useState<string | null>(null);

  const loadTenants = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setTenants(await getTenants());
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadTenants();
  }, [loadTenants]);

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setCreating(true);
    try {
      await createTenant(form);
      setForm(emptyForm);
      await loadTenants();
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setCreating(false);
    }
  }

  async function handleToggleActive(tenant: Tenant) {
    if (tenant.isActive && !confirm(t('tenants.confirmDeactivate', { name: tenant.name }))) {
      return;
    }

    setBusyTenantId(tenant.id);
    setError(null);
    try {
      await setTenantActive(tenant.id, !tenant.isActive);
      await loadTenants();
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setBusyTenantId(null);
    }
  }

  const [resetTenant, setResetTenant] = useState<Tenant | null>(null);

  return (
    <div className="page">
      <div className="page__header">
        <div>
          <h1>{t('tenants.title')}</h1>
          <p className="page__subtitle">{t('tenants.subtitle')}</p>
        </div>
      </div>

      <div className="panel">
        <h2>{t('tenants.addNew')}</h2>
        <form className="form" onSubmit={handleCreate}>
          <div className="form__row">
            <label className="form__field">
              <span>{t('tenants.name')}</span>
              <input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
            </label>
          </div>
          <p className="page__subtitle">{t('tenants.firstEmployerHint')}</p>
          <div className="form__row">
            <label className="form__field">
              <span>{t('employees.firstName')}</span>
              <input
                value={form.employerFirstName}
                onChange={(e) => setForm({ ...form, employerFirstName: e.target.value })}
                required
              />
            </label>
            <label className="form__field">
              <span>{t('employees.lastName')}</span>
              <input
                value={form.employerLastName}
                onChange={(e) => setForm({ ...form, employerLastName: e.target.value })}
                required
              />
            </label>
            <label className="form__field">
              <span>{t('employees.email')}</span>
              <input
                type="email"
                value={form.employerEmail}
                onChange={(e) => setForm({ ...form, employerEmail: e.target.value })}
                required
              />
            </label>
            <label className="form__field">
              <span>{t('employees.tempPassword')}</span>
              <input
                type="text"
                value={form.employerPassword}
                onChange={(e) => setForm({ ...form, employerPassword: e.target.value })}
                minLength={8}
                placeholder={t('employees.min8chars')}
                required
              />
            </label>
          </div>

          <div className="form__actions">
            <button className="btn btn--primary" type="submit" disabled={creating}>
              {creating ? t('employees.creating') : t('tenants.create')}
            </button>
          </div>
        </form>
      </div>

      {error && <p className="form__error">{error}</p>}

      {resetTenant && (
        <ResetEmployerPasswordPanel key={resetTenant.id} tenant={resetTenant} onClose={() => setResetTenant(null)} />
      )}

      {loading ? (
        <p className="empty-state">{t('common.loading')}</p>
      ) : tenants.length === 0 ? (
        <p className="empty-state">{t('tenants.emptyState')}</p>
      ) : (
        <div className="table-scroll">
          <table className="list-view">
            <thead>
              <tr>
                <th>{t('tenants.table.name')}</th>
                <th>{t('tenants.table.users')}</th>
                <th>{t('tenants.table.created')}</th>
                <th>{t('employees.table.status')}</th>
                <th aria-label={t('employees.table.actions')} />
              </tr>
            </thead>
            <tbody>
              {tenants.map((tenant) => (
                <tr key={tenant.id}>
                  <td>{tenant.name}</td>
                  <td>{tenant.userCount}</td>
                  <td>{new Date(tenant.createdAtUtc).toLocaleDateString(i18n.language)}</td>
                  <td>
                    <span className={tenant.isActive ? "status status--active" : "status status--inactive"}>
                      {tenant.isActive ? t('employees.statusActive') : t('employees.statusDeactivated')}
                    </span>
                  </td>
                  <td className="list-view__actions">
                    <RowMenu
                      disabled={busyTenantId === tenant.id}
                      items={[
                        { label: t('tenants.resetEmployerPassword'), onSelect: () => setResetTenant(tenant) },
                        {
                          label: tenant.isActive ? t('employees.deactivate') : t('employees.reactivate'),
                          onSelect: () => handleToggleActive(tenant),
                          danger: tenant.isActive
                        }
                      ]}
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

/**
 * For when a company's employer is locked out: pick one of its employers and give them a
 * temporary password, which they must replace at their next login.
 */
function ResetEmployerPasswordPanel({ tenant, onClose }: { tenant: Tenant; onClose: () => void }) {
  const { t } = useTranslation();
  const [employers, setEmployers] = useState<TenantEmployer[] | null>(null);
  const [employerId, setEmployerId] = useState("");
  const [password, setPassword] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [doneFor, setDoneFor] = useState<string | null>(null);

  useEffect(() => {
    getTenantEmployers(tenant.id)
      .then((list) => {
        setEmployers(list);
        const first = list.find((e) => e.isActive) ?? list[0];
        setEmployerId(first?.id ?? "");
      })
      .catch((err) => setError(extractErrorMessage(err)));
  }, [tenant.id]);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setDoneFor(null);
    setBusy(true);
    try {
      await resetEmployerPassword(tenant.id, employerId, password);
      setDoneFor(employers?.find((e) => e.id === employerId)?.fullName ?? "");
      setPassword("");
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="panel">
      <h2>{t('tenants.resetEmployerPasswordFor', { name: tenant.name })}</h2>
      <p className="form__hint">{t('tenants.resetEmployerPasswordHint')}</p>

      {employers === null ? (
        !error && <p className="empty-state">{t('common.loading')}</p>
      ) : employers.length === 0 ? (
        <p className="empty-state">{t('tenants.noEmployers')}</p>
      ) : (
        <form className="form" onSubmit={handleSubmit}>
          <div className="form__row">
            <label className="form__field">
              <span>{t('tenants.employer')}</span>
              <select value={employerId} onChange={(e) => setEmployerId(e.target.value)} required>
                {employers.map((e) => (
                  <option key={e.id} value={e.id}>
                    {e.fullName} ({e.email}){e.isActive ? "" : ` – ${t('employees.statusDeactivated')}`}
                  </option>
                ))}
              </select>
            </label>
            <label className="form__field">
              <span>{t('employees.newTempPassword')}</span>
              <input
                type="text"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                minLength={8}
                placeholder={t('employees.min8chars')}
                required
              />
            </label>
          </div>
          {doneFor !== null && <p className="form__success">{t('tenants.resetEmployerPasswordDone', { name: doneFor })}</p>}
          <div className="form__actions">
            <button className="btn btn--primary" type="submit" disabled={busy || !employerId}>
              {busy ? t('employees.saving') : t('employees.setNewPassword')}
            </button>
            <button type="button" className="btn btn--ghost" onClick={onClose}>
              {doneFor !== null ? t('common.close') : t('common.cancel')}
            </button>
          </div>
        </form>
      )}
      {error && <p className="form__error">{error}</p>}
    </div>
  );
}
