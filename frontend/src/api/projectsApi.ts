import type { Project, ProjectBreakdown } from "../types";
import { apiClient } from "./client";

export const projectsApi = {
    getAll: async (includeInactive = false): Promise<Project[]> => {
        const response = await apiClient.get<Project[]>(`/projects?includeInactive=${includeInactive}`);
        return response.data;
    },

    create: async (payload: { name: string; clientName: string | null }): Promise<string> => {
        const response = await apiClient.post<string>("/projects", payload);
        return response.data;
    },

    update: async (id: string, payload: { id: string; name: string; clientName: string | null }): Promise<void> => {
        await apiClient.put(`/projects/${id}`, payload);
    },

    toggleStatus: async (id: string, isActive: boolean): Promise<void> => {
        await apiClient.patch(`/projects/${id}/status`, isActive, {
            headers: { "Content-Type": "application/json" }
        });
    },

    getBreakdown: async (id: string): Promise<ProjectBreakdown[]> => {
        const response = await apiClient.get<ProjectBreakdown[]>(`/projects/${id}/breakdown`);
        return response.data;
    }
};