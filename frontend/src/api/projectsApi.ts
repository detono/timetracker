import type { Project, ProjectBreakdown, ProjectInput } from "../types";
import { apiClient } from "./client";

export const projectsApi = {
    getAll: async (includeInactive = false): Promise<Project[]> => {
        const response = await apiClient.get<Project[]>(`/projects?includeInactive=${includeInactive}`);
        return response.data;
    },

    create: async (payload: ProjectInput): Promise<string> => {
        const response = await apiClient.post<string>("/projects", payload);
        return response.data;
    },

    update: async (id: string, payload: ProjectInput): Promise<void> => {
        await apiClient.put(`/projects/${id}`, { id, ...payload });
    },

    toggleStatus: async (id: string, isActive: boolean): Promise<void> => {
        await apiClient.patch(`/projects/${id}/status`, isActive, {
            headers: { "Content-Type": "application/json" }
        });
    },

    /** Hours per person, over all time or only between the given dates (yyyy-MM-dd). */
    getBreakdown: async (id: string, from?: string, to?: string): Promise<ProjectBreakdown[]> => {
        const response = await apiClient.get<ProjectBreakdown[]>(`/projects/${id}/breakdown`, {
            params: { from: from || undefined, to: to || undefined }
        });
        return response.data;
    }
};
