import React, { useState, useEffect } from "react";
import type { Project } from "../types";
import { projectsApi } from "../api/projectsApi";

export const ProjectsPage: React.FC = () => {
    const [projects, setProjects] = useState<Project[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    // Form state
    const [isFormOpen, setIsFormOpen] = useState(false);
    const [editingId, setEditingId] = useState<string | null>(null);
    const [formData, setFormData] = useState({ name: "", clientName: "" });

    const loadProjects = async () => {
        try {
            setIsLoading(true);
            // Fetch ALL projects, including inactive ones, for the management view
            const data = await projectsApi.getAll(true);
            setProjects(data);
            setError(null);
        } catch (err) {
            setError("Failed to load projects.");
            console.error(err);
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        loadProjects();
    }, []);

    const handleOpenForm = (project?: Project) => {
        if (project) {
            setEditingId(project.id);
            setFormData({ name: project.name, clientName: project.clientName || "" });
        } else {
            setEditingId(null);
            setFormData({ name: "", clientName: "" });
        }
        setIsFormOpen(true);
    };

    const handleCloseForm = () => {
        setIsFormOpen(false);
        setEditingId(null);
        setFormData({ name: "", clientName: "" });
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        try {
            const payload = {
                name: formData.name,
                clientName: formData.clientName.trim() === "" ? null : formData.clientName,
            };

            if (editingId) {
                await projectsApi.update(editingId, { id: editingId, ...payload });
            } else {
                await projectsApi.create(payload);
            }

            handleCloseForm();
            await loadProjects();
        } catch (err) {
            setError("Failed to save the project.");
            console.error(err);
        }
    };

    const handleToggleStatus = async (id: string, currentStatus: boolean) => {
        try {
            await projectsApi.toggleStatus(id, !currentStatus);
            // Optimistically update the UI to avoid a full reload for a simple toggle
            setProjects((prev) =>
                prev.map((p) => (p.id === id ? { ...p, isActive: !currentStatus } : p))
            );
        } catch (err) {
            setError("Failed to update project status.");
            console.error(err);
        }
    };

    if (isLoading) return <p>Loading projects...</p>;

    return (
        <div className="projects-page">
            <header>
                <h2>Project Management</h2>
                <button onClick={() => handleOpenForm()}>Add New Project</button>
            </header>

            {error && <p className="error-message" style={{ color: "red" }}>{error}</p>}

            {isFormOpen && (
                <form onSubmit={handleSubmit} style={{ border: "1px solid #ccc", padding: "1rem", marginBottom: "1rem" }}>
                    <h3>{editingId ? "Edit Project" : "New Project"}</h3>

                    <div>
                        <label>Project Name*</label>
                        <input
                            type="text"
                            required
                            maxLength={100}
                            value={formData.name}
                            onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                        />
                    </div>

                    <div>
                        <label>Client Name (Optional)</label>
                        <input
                            type="text"
                            maxLength={100}
                            value={formData.clientName}
                            onChange={(e) => setFormData({ ...formData, clientName: e.target.value })}
                        />
                    </div>

                    <div style={{ marginTop: "1rem" }}>
                        <button type="submit">Save</button>
                        <button type="button" onClick={handleCloseForm}>Cancel</button>
                    </div>
                </form>
            )}

            <table>
                <thead>
                    <tr>
                        <th>Name</th>
                        <th>Client</th>
                        <th>Status</th>
                        <th>Actions</th>
                    </tr>
                </thead>
                <tbody>
                    {projects.map((project) => (
                        <tr key={project.id}>
                            <td>{project.name}</td>
                            <td>{project.clientName || "-"}</td>
                            <td>{project.isActive ? "Active" : "Archived"}</td>
                            <td>
                                <button onClick={() => handleOpenForm(project)}>Edit</button>
                                <button onClick={() => handleToggleStatus(project.id, project.isActive)}>
                                    {project.isActive ? "Archive" : "Reactivate"}
                                </button>
                            </td>
                        </tr>
                    ))}
                    {projects.length === 0 && (
                        <tr>
                            <td colSpan={4}>No projects found.</td>
                        </tr>
                    )}
                </tbody>
            </table>
        </div>
    );
};