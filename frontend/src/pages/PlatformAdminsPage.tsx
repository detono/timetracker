import { FormEvent, useCallback, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import {
  createPlatformAdmin,
  getPlatformAdmins,
  setPlatformAdminActive,
  type CreatePlatformAdminPayload
} from "../api/platformAdminsApi";
import { extractErrorMessage } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import type { UserAccount } from "../types";

const emptyForm: CreatePlatformAdminPayload = { firstName: "", lastName: "", email: "", password: "" };

export function PlatformAdminsPage() {
  const { t } = useTranslation();
  const { user: currentUser } = useAuth();
  const [admins, setAdmins] = useState<UserAccount[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [creating, setCreating] = useState(false);
  const [busyAdminId, setBusyAdminId] = useState<string | null>(null);

  const loadAdmins = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setAdmins(await getPlatformAdmins());
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadAdmins();
  }, [loadAdmins]);

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setCreating(true);
    try {
      await createPlatformAdmin(form);
      setForm(emptyForm);
      await loadAdmins();
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setCreating(false);
    }
  }

  async function handleToggleActive(admin: UserAccount) {
    const name = `${admin.firstName} ${admin.lastName}`;
    if (admin.isActive && !confirm(t('platformAdmins.confirmDeactivate', { name }))) {
      return;
    }

    setBusyAdminId(admin.id);
    setError(null);
    try {
      await setPlatformAdminActive(admin.id, !admin.isActive);
      await loadAdmins();
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setBusyAdminId(null);
    }
  }

  return (
    <div className="page">
      <div className="page__header">
        <div>
          <h1>{t('platformAdmins.title')}</h1>
          <p className="page__subtitle">{t('platformAdmins.subtitle')}</p>
        </div>
      </div>

      <div className="panel">
        <h2>{t('platformAdmins.addNew')}</h2>
        <form className="form" onSubmit={handleCreate}>
          <div className="form__row">
            <label className="form__field">
              <span>{t('employees.firstName')}</span>
              <input value={form.firstName} onChange={(e) => setForm({ ...form, firstName: e.target.value })} required />
            </label>
            <label className="form__field">
              <span>{t('employees.lastName')}</span>
              <input value={form.lastName} onChange={(e) => setForm({ ...form, lastName: e.target.value })} required />
            </label>
            <label className="form__field">
              <span>{t('employees.email')}</span>
              <input
                type="email"
                value={form.email}
                onChange={(e) => setForm({ ...form, email: e.target.value })}
                required
              />
            </label>
            <label className="form__field">
              <span>{t('employees.tempPassword')}</span>
              <input
                type="text"
                value={form.password}
                onChange={(e) => setForm({ ...form, password: e.target.value })}
                minLength={8}
                placeholder={t('employees.min8chars')}
                required
              />
            </label>
          </div>

          <div className="form__actions">
            <button className="btn btn--primary" type="submit" disabled={creating}>
              {creating ? t('employees.creating') : t('employees.createAccount')}
            </button>
          </div>
        </form>
      </div>

      {error && <p className="form__error">{error}</p>}

      {loading ? (
        <p className="empty-state">{t('common.loading')}</p>
      ) : (
        <table className="list-view">
          <thead>
            <tr>
              <th>{t('employees.table.name')}</th>
              <th>{t('employees.table.email')}</th>
              <th>{t('employees.table.status')}</th>
              <th aria-label={t('employees.table.actions')} />
            </tr>
          </thead>
          <tbody>
            {admins.map((admin) => (
              <tr key={admin.id}>
                <td>
                  {admin.firstName} {admin.lastName}
                  {admin.id === currentUser?.userId && <span className="badge">{t('employees.youBadge')}</span>}
                </td>
                <td>{admin.email}</td>
                <td>
                  <span className={admin.isActive ? "status status--active" : "status status--inactive"}>
                    {admin.isActive ? t('employees.statusActive') : t('employees.statusDeactivated')}
                  </span>
                </td>
                <td className="list-view__actions">
                  <button
                    className={admin.isActive ? "btn btn--ghost btn--sm btn--danger" : "btn btn--ghost btn--sm"}
                    onClick={() => handleToggleActive(admin)}
                    disabled={busyAdminId === admin.id || admin.id === currentUser?.userId}
                    title={admin.id === currentUser?.userId ? t('employees.cannotDeactivateSelf') : undefined}
                  >
                    {admin.isActive ? t('employees.deactivate') : t('employees.reactivate')}
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
