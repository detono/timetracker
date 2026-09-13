import { apiClient } from "./client";
import type { HourType } from "../types";

export async function getHourTypes(includeInactive = false): Promise<HourType[]> {
  const { data } = await apiClient.get<HourType[]>("/hourtypes", { params: { includeInactive } });
  return data;
}

export async function createHourType(localizedNames: Record<string, string>, colorHex: string, isDefault: boolean): Promise<HourType> {
  const { data } = await apiClient.post<HourType>("/hourtypes", { localizedNames, colorHex, isDefault });
  return data;
}

export async function updateHourType(id: string, localizedNames: Record<string, string>, colorHex: string, isDefault: boolean): Promise<HourType> {
  const { data } = await apiClient.put<HourType>(`/hourtypes/${id}`, { id, localizedNames, colorHex, isDefault });
  return data;
}

export async function deactivateHourType(id: string): Promise<void> {
  await apiClient.delete(`/hourtypes/${id}`);
}

export async function activateHourType(id: string): Promise<void> {
  await apiClient.post(`/hourtypes/${id}/activate`);
}
