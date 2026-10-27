import { apiClient } from "./client";
import type { UserAccount } from "../types";

export interface CreatePlatformAdminPayload {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

export async function getPlatformAdmins(): Promise<UserAccount[]> {
  const { data } = await apiClient.get<UserAccount[]>("/platform-admins");
  return data;
}

export async function createPlatformAdmin(payload: CreatePlatformAdminPayload): Promise<UserAccount> {
  const { data } = await apiClient.post<UserAccount>("/platform-admins", payload);
  return data;
}

export async function setPlatformAdminActive(adminId: string, isActive: boolean): Promise<void> {
  await apiClient.post(`/platform-admins/${adminId}/${isActive ? "activate" : "deactivate"}`);
}
