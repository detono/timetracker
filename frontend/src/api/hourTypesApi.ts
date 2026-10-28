import { apiClient } from "./client";
import type { HourType } from "../types";

export async function getHourTypes(includeInactive = false): Promise<HourType[]> {
  const { data } = await apiClient.get<HourType[]>("/hourtypes", { params: { includeInactive } });
  return data;
}

export interface HourTypePayload {
  localizedNames: Record<string, string>;
  colorHex: string;
  isDefault: boolean;
  isLeave: boolean;
  deductsFromLeaveBalance: boolean;
}

export async function createHourType(payload: HourTypePayload): Promise<HourType> {
  const { data } = await apiClient.post<HourType>("/hourtypes", payload);
  return data;
}

export async function updateHourType(id: string, payload: HourTypePayload): Promise<HourType> {
  const { data } = await apiClient.put<HourType>(`/hourtypes/${id}`, { id, ...payload });
  return data;
}

export async function deactivateHourType(id: string): Promise<void> {
  await apiClient.delete(`/hourtypes/${id}`);
}

export async function activateHourType(id: string): Promise<void> {
  await apiClient.post(`/hourtypes/${id}/activate`);
}
