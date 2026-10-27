import { FormEvent, useCallback, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { createTenant, getTenants, setTenantActive, type CreateTenantPayload } from "../api/tenantsApi";
import { extractErrorMessage } from "../api/client";
import type { Tenant } from "../types";

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

      {loading ? (
        <p className="empty-state">{t('common.loading')}</p>
      ) : tenants.length === 0 ? (
        <p className="empty-state">{t('tenants.emptyState')}</p>
      ) : (
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
                  <button
                    className={tenant.isActive ? "btn btn--ghost btn--sm btn--danger" : "btn btn--ghost btn--sm"}
                    onClick={() => handleToggleActive(tenant)}
                    disabled={busyTenantId === tenant.id}
                  >
                    {tenant.isActive ? t('employees.deactivate') : t('employees.reactivate')}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
