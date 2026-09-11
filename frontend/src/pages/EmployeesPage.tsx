import { FormEvent, useCallback, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import {
  activateUser,
  assignSupervisor,
  createUser,
  deactivateUser,
  getAllUsers,
  resetPassword
} from "../api/usersApi";
import { extractErrorMessage } from "../api/client";
import { useAuth } from "../auth/AuthContext";
import type { UserAccount, UserRole } from "../types";

const emptyForm = {
  firstName: "",
  lastName: "",
  email: "",
  password: "",
  role: "Employee" as UserRole
};

export function EmployeesPage() {
  const { t } = useTranslation();
  const { user: currentUser } = useAuth();
  const [users, setUsers] = useState<UserAccount[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [creating, setCreating] = useState(false);
  const [busyUserId, setBusyUserId] = useState<string | null>(null);
  const [resettingUser, setResettingUser] = useState<UserAccount | null>(null);
  const [resetPasswordValue, setResetPasswordValue] = useState("");
  const [resetSubmitting, setResetSubmitting] = useState(false);

  const loadUsers = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getAllUsers();
      setUsers(data);
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadUsers();
  }, [loadUsers]);

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setCreating(true);
    try {
      await createUser(form);
      setForm(emptyForm);
      await loadUsers();
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setCreating(false);
    }
  }

  async function handleToggleActive(target: UserAccount) {
    setBusyUserId(target.id);
    setError(null);
    try {
      if (target.isActive) {
        if (!confirm(t('employees.confirmDeactivate', { name: `${target.firstName} ${target.lastName}` }))) {
          return;
        }
        await deactivateUser(target.id);
      } else {
        await activateUser(target.id);
      }
      await loadUsers();
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setBusyUserId(null);
    }
  }

  async function handleSupervisorChange(target: UserAccount, supervisorId: string) {
    setBusyUserId(target.id);
    setError(null);
    try {
      await assignSupervisor(target.id, supervisorId || null);
      await loadUsers();
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setBusyUserId(null);
    }
  }

  const potentialSupervisors = users.filter((u) => u.role === "Employee");

  async function handleResetPassword(e: FormEvent) {
    e.preventDefault();
    if (!resettingUser) return;
    setError(null);
    setResetSubmitting(true);
    try {
      await resetPassword(resettingUser.id, resetPasswordValue);
      setResettingUser(null);
      setResetPasswordValue("");
    } catch (err) {
      setError(extractErrorMessage(err));
    } finally {
      setResetSubmitting(false);
    }
  }

  return (
    <div className="page">
      <div className="page__header">
        <div>
          <h1>{t('employees.title')}</h1>
          <p className="page__subtitle">{t('employees.subtitle')}</p>
        </div>
      </div>

      <div className="panel">
        <h2>{t('employees.addNew')}</h2>
        <form className="form" onSubmit={handleCreate}>
          <div className="form__row">
            <label className="form__field">
              <span>{t('employees.firstName')}</span>
              <input
                value={form.firstName}
                onChange={(e) => setForm({ ...form, firstName: e.target.value })}
                required
              />
            </label>
            <label className="form__field">
              <span>{t('employees.lastName')}</span>
              <input
                value={form.lastName}
                onChange={(e) => setForm({ ...form, lastName: e.target.value })}
                required
              />
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
                placeholder={t('employees.min8chars')}
                required
              />
            </label>
            <label className="form__field form__field--narrow">
              <span>{t('employees.role')}</span>
              <select value={form.role} onChange={(e) => setForm({ ...form, role: e.target.value as UserRole })}>
                <option value="Employee">{t('roles.employee')}</option>
                <option value="Employer">{t('roles.employer')}</option>
              </select>
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

      {resettingUser && (
        <div className="panel">
          <h2>{t('employees.resetPasswordFor', { name: `${resettingUser.firstName} ${resettingUser.lastName}` })}</h2>
          <form className="form" onSubmit={handleResetPassword}>
            <label className="form__field">
              <span>{t('employees.newTempPassword')}</span>
              <input
                type="text"
                value={resetPasswordValue}
                onChange={(e) => setResetPasswordValue(e.target.value)}
                minLength={8}
                placeholder={t('employees.min8chars')}
                required
              />
            </label>
            <div className="form__actions">
              <button className="btn btn--primary" type="submit" disabled={resetSubmitting}>
                {resetSubmitting ? t('employees.saving') : t('employees.setNewPassword')}
              </button>
              <button
                type="button"
                className="btn btn--ghost"
                onClick={() => {
                  setResettingUser(null);
                  setResetPasswordValue("");
                }}
              >
                {t('common.cancel')}
              </button>
            </div>
          </form>
        </div>
      )}

      {loading ? (
        <p className="empty-state">{t('common.loading')}</p>
      ) : (
        <table className="list-view">
          <thead>
            <tr>
              <th>{t('employees.table.name')}</th>
              <th>{t('employees.table.email')}</th>
              <th>{t('employees.table.role')}</th>
              <th>{t('employees.table.supervisor')}</th>
              <th>{t('employees.table.status')}</th>
              <th aria-label={t('employees.table.actions')} />
            </tr>
          </thead>
          <tbody>
            {users.map((u) => (
              <tr key={u.id}>
                <td>
                  {u.firstName} {u.lastName}
                  {u.id === currentUser?.userId && <span className="badge">{t('employees.youBadge')}</span>}
                </td>
                <td>{u.email}</td>
                <td>{u.role === "Employer" ? t('roles.employer') : t('roles.employee')}</td>
                <td>
                  {u.role === "Employee" ? (
                    <select
                      value={u.supervisorId ?? ""}
                      onChange={(e) => handleSupervisorChange(u, e.target.value)}
                      disabled={busyUserId === u.id}
                    >
                      <option value="">{t('employees.none')}</option>
                      {potentialSupervisors
                        .filter((s) => s.id !== u.id)
                        .map((s) => (
                          <option key={s.id} value={s.id}>
                            {s.firstName} {s.lastName}
                          </option>
                        ))}
                    </select>
                  ) : (
                    <span className="list-view__notes">—</span>
                  )}
                </td>
                <td>
                  <span className={u.isActive ? "status status--active" : "status status--inactive"}>
                    {u.isActive ? t('employees.statusActive') : t('employees.statusDeactivated')}
                  </span>
                </td>
                <td className="list-view__actions">
                  <button
                    className="btn btn--ghost btn--sm"
                    onClick={() => {
                      setResettingUser(u);
                      setResetPasswordValue("");
                    }}
                  >
                    {t('employees.btnResetPassword')}
                  </button>
                  <button
                    className={u.isActive ? "btn btn--ghost btn--sm btn--danger" : "btn btn--ghost btn--sm"}
                    onClick={() => handleToggleActive(u)}
                    disabled={busyUserId === u.id || u.id === currentUser?.userId}
                    title={u.id === currentUser?.userId ? t('employees.cannotDeactivateSelf') : undefined}
                  >
                    {u.isActive ? t('employees.deactivate') : t('employees.reactivate')}
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