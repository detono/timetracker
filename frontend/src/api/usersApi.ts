import { apiClient } from "./client";
import type { UserAccount, UserRole } from "../types";

export async function getAllUsers(): Promise<UserAccount[]> {
  const { data } = await apiClient.get<UserAccount[]>("/users");
  return data;
}

export interface CreateUserPayload {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  role: UserRole;
}

export async function createUser(payload: CreateUserPayload): Promise<UserAccount> {
  const { data } = await apiClient.post<UserAccount>("/users", payload);
  return data;
}

export async function deactivateUser(userId: string): Promise<void> {
  await apiClient.delete(`/users/${userId}`);
}

export async function activateUser(userId: string): Promise<void> {
  await apiClient.post(`/users/${userId}/activate`);
}

export async function assignSupervisor(employeeId: string, supervisorId: string | null): Promise<void> {
  await apiClient.post(`/users/${employeeId}/supervisor`, supervisorId);
}

export async function resetPassword(userId: string, newPassword: string): Promise<void> {
  await apiClient.post(`/users/${userId}/reset-password`, { newPassword });
}
