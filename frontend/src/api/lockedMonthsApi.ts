import { apiClient } from "./client";
import type { LockedMonth } from "../types";

export async function getLockedMonths(): Promise<LockedMonth[]> {
  const { data } = await apiClient.get<LockedMonth[]>("/locked-months");
  return data;
}

/** Employer only. Nobody can change the month's hours until it's unlocked. */
export async function lockMonth(year: number, month: number): Promise<LockedMonth> {
  const { data } = await apiClient.put<LockedMonth>(`/locked-months/${year}/${month}`);
  return data;
}

export async function unlockMonth(year: number, month: number): Promise<void> {
  await apiClient.delete(`/locked-months/${year}/${month}`);
}
