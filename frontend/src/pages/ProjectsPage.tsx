import { FormEvent, Fragment, useCallback, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { projectsApi } from "../api/projectsApi";
import { getAllUsers } from "../api/usersApi";
import { extractErrorMessage } from "../api/client";
import type { Project, ProjectBreakdown, UserAccount } from "../types";
import { RowMenu } from "../components/RowMenu";
import { hoursToHm } from "../utils/dateRange";

const emptyForm = {
    name: "",
    clientName: "",
    budgetHours: "",
    isBillable: false,
    /** "everyone", or only the people in memberIds. */
    access: "everyone" as "everyone" | "members",
    memberIds: [] as string[]
};

type ProjectForm = typeof emptyForm;

const fullName = (u: UserAccount) => `${u.firstName} ${u.lastName}`;

export function ProjectsPage() {
    const { t } = useTranslation();
    const [projects, setProjects] = useState<Project[]>([]);
    const [people, setPeople] = useState<UserAccount[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const [form, setForm] = useState<ProjectForm>(emptyForm);
    const [saving, setSaving] = useState(false);
    const [busyProjectId, setBusyProjectId] = useState<string | null>(null);
    const [editingProject, setEditingProject] = useState<Project | null>(null);

    const [expandedId, setExpandedId] = useState<string | null>(null);

    const loadProjects = useCallback(async () => {
        setLoading(true);
        setError(null);
        try {
            const [data, users] = await Promise.all([projectsApi.getAll(true), getAllUsers()]);
            setProjects(data);
            setPeople(users);
        } catch (err) {
            setError(extractErrorMessage(err));
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        loadProjects();
    }, [loadProjects]);

    async function handleSubmit(e: FormEvent) {
        e.preventDefault();
        setError(null);

        if (form.access === "members" && form.memberIds.length === 0) {
            setError(t('projects.pickMembers'));
            return;
        }

        setSaving(true);
        try {
            const payload = {
                name: form.name,
                clientName: form.clientName.trim() === "" ? null : form.clientName,
                budgetHours: form.budgetHours.trim() === "" ? null : Number(form.budgetHours),
                isBillable: form.isBillable,
                memberIds: form.access === "members" ? form.memberIds : []
            };

            if (editingProject) {
                await projectsApi.update(editingProject.id, payload);
            } else {
                await projectsApi.create(payload);
            }

            setForm(emptyForm);
            setEditingProject(null);
            await loadProjects();
        } catch (err) {
            setError(extractErrorMessage(err));
        } finally {
            setSaving(false);
        }
    }

    function handleEditClick(project: Project) {
        setEditingProject(project);
        setForm({
            name: project.name,
            clientName: project.clientName || "",
            budgetHours: project.budgetHours?.toString() ?? "",
            isBillable: project.isBillable,
            access: project.memberIds.length > 0 ? "members" : "everyone",
            memberIds: project.memberIds
        });
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    function cancelEdit() {
        setEditingProject(null);
        setForm(emptyForm);
        setError(null);
    }

    function toggleMember(userId: string, checked: boolean) {
        setForm((f) => ({
            ...f,
            memberIds: checked ? [...f.memberIds, userId] : f.memberIds.filter((id) => id !== userId)
        }));
    }

    async function handleToggleActive(target: Project) {
        setBusyProjectId(target.id);
        setError(null);
        try {
            await projectsApi.toggleStatus(target.id, !target.isActive);
            await loadProjects();
        } catch (err) {
            setError(extractErrorMessage(err));
        } finally {
            setBusyProjectId(null);
        }
    }

    // Active people, plus anyone who left but is still on the project being edited.
    const pickable = people
        .filter((u) => u.isActive || form.memberIds.includes(u.id))
        .sort((a, b) => fullName(a).localeCompare(fullName(b)));

    return (
        <div className="page">
            <div className="page__header">
                <div>
                    <h1>{t('projects.title')}</h1>
                    <p className="page__subtitle">{t('projects.subtitle')}</p>
                </div>
            </div>

            <div className="panel">
                <h2>{editingProject ? t('projects.editProject') : t('projects.addNew')}</h2>
                <form className="form" onSubmit={handleSubmit}>
                    <div className="form__row">
                        <label className="form__field">
                            <span>{t('projects.name')}</span>
                            <input
                                value={form.name}
                                onChange={(e) => setForm({ ...form, name: e.target.value })}
                                maxLength={100}
                                required
                            />
                        </label>
                        <label className="form__field">
                            <span>{t('projects.clientName')}</span>
                            <input
                                value={form.clientName}
                                onChange={(e) => setForm({ ...form, clientName: e.target.value })}
                                placeholder={t('projects.optional')}
                                maxLength={100}
                            />
                        </label>
                        <label className="form__field form__field--narrow">
                            <span>{t('projects.budgetHours')}</span>
                            <input
                                type="number"
                                inputMode="decimal"
                                min={0.5}
                                max={100000}
                                step={0.5}
                                value={form.budgetHours}
                                onChange={(e) => setForm({ ...form, budgetHours: e.target.value })}
                                placeholder={t('projects.optional')}
                            />
                        </label>
                    </div>

                    <div className="form__row form__row--options">
                        <label className="check-field">
                            <input
                                type="checkbox"
                                checked={form.isBillable}
                                onChange={(e) => setForm({ ...form, isBillable: e.target.checked })}
                            />
                            {t('projects.billable')}
                        </label>
                    </div>

                    <fieldset className="project-access">
                        <legend>{t('projects.whoCanLog')}</legend>
                        <label className="check-field">
                            <input
                                type="radio"
                                name="project-access"
                                checked={form.access === "everyone"}
                                onChange={() => setForm({ ...form, access: "everyone" })}
                            />
                            {t('projects.accessEveryone')}
                        </label>
                        <label className="check-field">
                            <input
                                type="radio"
                                name="project-access"
                                checked={form.access === "members"}
                                onChange={() => setForm({ ...form, access: "members" })}
                            />
                            {t('projects.accessMembers')}
                        </label>
                        {form.access === "members" && (
                            <div className="member-picker">
                                {pickable.map((u) => (
                                    <label key={u.id} className="check-field">
                                        <input
                                            type="checkbox"
                                            checked={form.memberIds.includes(u.id)}
                                            onChange={(e) => toggleMember(u.id, e.target.checked)}
                                        />
                                        {fullName(u)}
                                        {!u.isActive && <span className="list-view__notes">({t('employees.statusDeactivated')})</span>}
                                    </label>
                                ))}
                            </div>
                        )}
                    </fieldset>

                    <div className="form__actions">
                        <button className="btn btn--primary" type="submit" disabled={saving}>
                            {saving
                                ? t('common.saving')
                                : (editingProject ? t('common.saveChanges') : t('projects.createProject'))}
                        </button>
                        {editingProject && (
                            <button
                                type="button"
                                className="btn btn--ghost"
                                onClick={cancelEdit}
                                disabled={saving}
                            >
                                {t('common.cancel')}
                            </button>
                        )}
                    </div>
                </form>
            </div>

            {error && <p className="form__error">{error}</p>}

            {loading ? (
                <p className="empty-state">{t('common.loading')}</p>
            ) : (
                <div className="table-scroll">
                  <table className="list-view">
                      <thead>
                          <tr>
                              <th>{t('projects.table.name')}</th>
                              <th>{t('projects.table.client')}</th>
                              <th>{t('projects.table.thisMonth')}</th>
                              <th>{t('projects.table.lifetime')}</th>
                              <th>{t('projects.table.budget')}</th>
                              <th>{t('projects.table.status')}</th>
                              <th aria-label={t('projects.table.actions')} />
                          </tr>
                      </thead>
                      <tbody>
                          {projects.map((p) => (
                              <Fragment key={p.id}>
                                  <tr>
                                      <td>
                                          <div className="project-cell">
                                              <span>{p.name}</span>
                                              <span className="list-view__notes">
                                                  {[
                                                      p.isBillable ? t('projects.billable') : null,
                                                      p.memberIds.length > 0
                                                          ? t('projects.onlyPeople', { count: p.memberIds.length })
                                                          : null
                                                  ].filter(Boolean).join(" · ")}
                                              </span>
                                          </div>
                                      </td>
                                      <td>{p.clientName ? p.clientName : <span className="list-view__notes">—</span>}</td>
                                      <td>{hoursToHm(p.totalHoursThisMonth)}</td>
                                      <td>{hoursToHm(p.totalHoursLifetime)}</td>
                                      <td><BudgetCell project={p} /></td>
                                      <td>
                                          <span className={p.isActive ? "status status--active" : "status status--inactive"}>
                                              {p.isActive ? t('projects.statusActive') : t('projects.statusArchived')}
                                          </span>
                                      </td>
                                      <td className="list-view__actions">
                                          <RowMenu
                                              disabled={busyProjectId === p.id}
                                              items={[
                                                  {
                                                      label: expandedId === p.id ? t('projects.hideDetails') : t('projects.viewDetails'),
                                                      onSelect: () => setExpandedId(expandedId === p.id ? null : p.id)
                                                  },
                                                  { label: t('common.edit'), onSelect: () => handleEditClick(p) },
                                                  {
                                                      label: p.isActive ? t('projects.archive') : t('projects.reactivate'),
                                                      onSelect: () => handleToggleActive(p),
                                                      danger: p.isActive
                                                  }
                                              ]}
                                          />
                                      </td>
                                  </tr>

                                  {expandedId === p.id && (
                                      <tr className="project-details">
                                          <td colSpan={7}>
                                              <ProjectBreakdownPanel projectId={p.id} />
                                          </td>
                                      </tr>
                                  )}
                              </Fragment>
                          ))}
                          {projects.length === 0 && (
                              <tr>
                                  <td colSpan={7} className="empty-state" style={{ textAlign: "center", padding: "2rem" }}>
                                      {t('projects.noProjects')}
                                  </td>
                              </tr>
                          )}
                      </tbody>
                  </table>
                </div>
            )}
        </div>
    );
}

/** "70% of 120h" with a bar, red once the hours go over the budget. */
function BudgetCell({ project }: { project: Project }) {
    const { t } = useTranslation();
    if (project.budgetHours == null) {
        return <span className="list-view__notes">—</span>;
    }

    const used = project.totalHoursLifetime / project.budgetHours;
    const over = used > 1;
    return (
        <div className={over ? "budget budget--over" : "budget"}>
            <span>
                {t('projects.budgetUsed', { percent: Math.round(used * 100), budget: hoursToHm(project.budgetHours) })}
            </span>
            <span className="progress" aria-hidden="true">
                <span className="progress__bar" style={{ width: `${Math.min(used, 1) * 100}%` }} />
            </span>
        </div>
    );
}

/** Hours per person on one project, over all time or between two dates. */
function ProjectBreakdownPanel({ projectId }: { projectId: string }) {
    const { t } = useTranslation();
    const [from, setFrom] = useState("");
    const [to, setTo] = useState("");
    const [rows, setRows] = useState<ProjectBreakdown[] | null>(null);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;
        setRows(null);
        setError(null);
        projectsApi.getBreakdown(projectId, from, to)
            .then((data) => { if (!cancelled) setRows(data); })
            .catch((err) => { if (!cancelled) setError(extractErrorMessage(err)); });
        return () => { cancelled = true; };
    }, [projectId, from, to]);

    const total = rows?.reduce((sum, r) => sum + r.totalHours, 0) ?? 0;

    return (
        <div className="project-breakdown">
            <div className="project-breakdown__head">
                <h4>{t('projects.breakdownTitle')}</h4>
                <div className="project-breakdown__dates">
                    <label className="form__field form__field--narrow">
                        <span>{t('reports.from')}</span>
                        <input type="date" value={from} max={to || undefined} onChange={(e) => setFrom(e.target.value)} />
                    </label>
                    <label className="form__field form__field--narrow">
                        <span>{t('reports.to')}</span>
                        <input type="date" value={to} min={from || undefined} onChange={(e) => setTo(e.target.value)} />
                    </label>
                    {(from || to) && (
                        <button type="button" className="btn btn--ghost btn--sm" onClick={() => { setFrom(""); setTo(""); }}>
                            {t('projects.allTime')}
                        </button>
                    )}
                </div>
            </div>
            {error && <p className="form__error">{error}</p>}
            {rows === null && !error ? (
                <p className="list-view__notes">{t('projects.loadingBreakdown')}</p>
            ) : rows && (
                <table className="list-view">
                    <thead>
                        <tr>
                            <th>{t('projects.employee')}</th>
                            <th>{t('projects.totalHours')}</th>
                        </tr>
                    </thead>
                    <tbody>
                        {rows.map((b) => (
                            <tr key={b.userId}>
                                <td>{b.employeeName}</td>
                                <td>{hoursToHm(b.totalHours)}</td>
                            </tr>
                        ))}
                        {rows.length === 0 ? (
                            <tr><td colSpan={2} className="empty-state">{t('projects.noHoursYet')}</td></tr>
                        ) : (
                            <tr className="project-breakdown__total">
                                <td>{t('projects.total')}</td>
                                <td>{hoursToHm(total)}</td>
                            </tr>
                        )}
                    </tbody>
                </table>
            )}
        </div>
    );
}
