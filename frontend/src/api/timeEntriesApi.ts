import { apiClient } from "./client";
import type { TimeEntry } from "../types";

export interface CreateTimeEntryPayload {
  targetUserId?: string | null;
  projectId: string | null;
  hourTypeId: string;
  workDate: string;
  startTime: string;
  endTime: string;
  breakMinutes: number;
  notes?: string | null;
}

export async function getMyTimeEntries(userId?: string, from?: string, to?: string): Promise<TimeEntry[]> {
  const { data } = await apiClient.get<TimeEntry[]>("/timeentries", { params: { userId, from, to } });
  return data;
}

export async function getTeamTimeEntries(from?: string, to?: string): Promise<TimeEntry[]> {
  const { data } = await apiClient.get<TimeEntry[]>("/timeentries/team", { params: { from, to } });
  return data;
}

export async function createTimeEntry(payload: CreateTimeEntryPayload): Promise<TimeEntry> {
  const { data } = await apiClient.post<TimeEntry>("/timeentries", payload);
  return data;
}

export async function updateTimeEntry(id: string, payload: Omit<CreateTimeEntryPayload, "targetUserId">): Promise<TimeEntry> {
  const { data } = await apiClient.put<TimeEntry>(`/timeentries/${id}`, { id, ...payload });
  return data;
}

export async function deleteTimeEntry(id: string): Promise<void> {
  await apiClient.delete(`/timeentries/${id}`);
}
