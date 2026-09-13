import { FormEvent, useCallback, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { projectsApi } from "../api/projectsApi";
import { extractErrorMessage } from "../api/client";
import type { Project } from "../types";

const emptyForm = {
    name: "",
    clientName: ""
};

export function ProjectsPage() {
    const { t } = useTranslation();
    const [projects, setProjects] = useState<Project[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    const [form, setForm] = useState(emptyForm);
    const [saving, setSaving] = useState(false);
    const [busyProjectId, setBusyProjectId] = useState<string | null>(null);
    const [editingProject, setEditingProject] = useState<Project | null>(null);

    const loadProjects = useCallback(async () => {
        setLoading(true);
        setError(null);
        try {
            const data = await projectsApi.getAll(true);
            setProjects(data);
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
        setSaving(true);

        try {
            const payload = {
                name: form.name,
                clientName: form.clientName.trim() === "" ? null : form.clientName,
            };

            if (editingProject) {
                await projectsApi.update(editingProject.id, { id: editingProject.id, ...payload });
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
            clientName: project.clientName || ""
        });
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }

    function cancelEdit() {
        setEditingProject(null);
        setForm(emptyForm);
        setError(null);
    }

    async function handleToggleActive(target: Project) {
        setBusyProjectId(target.id);
        setError(null);
        try {
            if (target.isActive) {
                if (!confirm(t('projects.confirmDeactivate', { name: target.name }))) {
                    return;
                }
            }

            await projectsApi.toggleStatus(target.id, !target.isActive);
            await loadProjects();
        } catch (err) {
            setError(extractErrorMessage(err));
        } finally {
            setBusyProjectId(null);
        }
    }

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
                    </div>

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
                <table className="list-view">
                    <thead>
                        <tr>
                            <th>{t('projects.table.name')}</th>
                            <th>{t('projects.table.client')}</th>
                            <th>{t('projects.table.status')}</th>
                            <th aria-label={t('projects.table.actions')} />
                        </tr>
                    </thead>
                    <tbody>
                        {projects.map((p) => (
                            <tr key={p.id}>
                                <td>{p.name}</td>
                                <td>{p.clientName ? p.clientName : <span className="list-view__notes">—</span>}</td>
                                <td>
                                    <span className={p.isActive ? "status status--active" : "status status--inactive"}>
                                        {p.isActive ? t('projects.statusActive') : t('projects.statusArchived')}
                                    </span>
                                </td>
                                <td className="list-view__actions">
                                    <button
                                        className="btn btn--ghost btn--sm"
                                        onClick={() => handleEditClick(p)}
                                        disabled={busyProjectId === p.id}
                                    >
                                        {t('common.edit')}
                                    </button>
                                    <button
                                        className={p.isActive ? "btn btn--ghost btn--sm btn--danger" : "btn btn--ghost btn--sm"}
                                        onClick={() => handleToggleActive(p)}
                                        disabled={busyProjectId === p.id}
                                    >
                                        {p.isActive ? t('projects.archive') : t('projects.reactivate')}
                                    </button>
                                </td>
                            </tr>
                        ))}
                        {projects.length === 0 && (
                            <tr>
                                <td colSpan={4} className="empty-state" style={{ textAlign: "center", padding: "2rem" }}>
                                    {t('projects.noProjects')}
                                </td>
                            </tr>
                        )}
                    </tbody>
                </table>
            )}
        </div>
    );
}