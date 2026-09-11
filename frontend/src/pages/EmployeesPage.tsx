import { FormEvent, useCallback, useEffect, useState } from "react";
import {
  activateUser,
  assignSupervisor,
  createUser,
  deactivateUser,
  getAllUsers
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
  const { user: currentUser } = useAuth();
  const [users, setUsers] = useState<UserAccount[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [creating, setCreating] = useState(false);
  const [busyUserId, setBusyUserId] = useState<string | null>(null);

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
        if (!confirm(`Deactivate ${target.firstName} ${target.lastName}? They will no longer be able to log in, but their logged hours are kept.`)) {
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

  return (
    <div className="page">
      <div className="page__header">
        <div>
          <h1>Manage employees</h1>
          <p className="page__subtitle">Create accounts, deactivate leavers, and assign supervisors.</p>
        </div>
      </div>

      <div className="panel">
        <h2>Add a new account</h2>
        <form className="form" onSubmit={handleCreate}>
          <div className="form__row">
            <label className="form__field">
              <span>First name</span>
              <input
                value={form.firstName}
                onChange={(e) => setForm({ ...form, firstName: e.target.value })}
                required
              />
            </label>
            <label className="form__field">
              <span>Last name</span>
              <input
                value={form.lastName}
                onChange={(e) => setForm({ ...form, lastName: e.target.value })}
                required
              />
            </label>
            <label className="form__field">
              <span>Email</span>
              <input
                type="email"
                value={form.email}
                onChange={(e) => setForm({ ...form, email: e.target.value })}
                required
              />
            </label>
            <label className="form__field">
              <span>Temporary password</span>
              <input
                type="text"
                value={form.password}
                onChange={(e) => setForm({ ...form, password: e.target.value })}
                placeholder="min. 8 characters"
                required
              />
            </label>
            <label className="form__field form__field--narrow">
              <span>Role</span>
              <select value={form.role} onChange={(e) => setForm({ ...form, role: e.target.value as UserRole })}>
                <option value="Employee">Employee</option>
                <option value="Employer">Employer</option>
              </select>
            </label>
          </div>

          <div className="form__actions">
            <button className="btn btn--primary" type="submit" disabled={creating}>
              {creating ? "Creating…" : "Create account"}
            </button>
          </div>
        </form>
      </div>

      {error && <p className="form__error">{error}</p>}

      {loading ? (
        <p className="empty-state">Loading…</p>
      ) : (
        <table className="list-view">
          <thead>
            <tr>
              <th>Name</th>
              <th>Email</th>
              <th>Role</th>
              <th>Supervisor</th>
              <th>Status</th>
              <th aria-label="Actions" />
            </tr>
          </thead>
          <tbody>
            {users.map((u) => (
              <tr key={u.id}>
                <td>
                  {u.firstName} {u.lastName}
                  {u.id === currentUser?.userId && <span className="badge">You</span>}
                </td>
                <td>{u.email}</td>
                <td>{u.role}</td>
                <td>
                  {u.role === "Employee" ? (
                    <select
                      value={u.supervisorId ?? ""}
                      onChange={(e) => handleSupervisorChange(u, e.target.value)}
                      disabled={busyUserId === u.id}
                    >
                      <option value="">— none —</option>
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
                    {u.isActive ? "Active" : "Deactivated"}
                  </span>
                </td>
                <td className="list-view__actions">
                  <button
                    className={u.isActive ? "btn btn--ghost btn--sm btn--danger" : "btn btn--ghost btn--sm"}
                    onClick={() => handleToggleActive(u)}
                    disabled={busyUserId === u.id || u.id === currentUser?.userId}
                    title={u.id === currentUser?.userId ? "You cannot deactivate your own account" : undefined}
                  >
                    {u.isActive ? "Deactivate" : "Reactivate"}
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
