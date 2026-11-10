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
  // The API takes a bare JSON value ("<id>" or null). Axios would send a plain string
  // unquoted as form data, which the API rejects as an unsupported format.
  await apiClient.post(`/users/${employeeId}/supervisor`, JSON.stringify(supervisorId), {
    headers: { "Content-Type": "application/json" }
  });
}

/** Whether someone's logged hours are compared with their work schedule. */
export async function setTracksHours(userId: string, tracksHours: boolean): Promise<void> {
  await apiClient.put(`/users/${userId}/tracks-hours`, { tracksHours });
}

export async function resetPassword(userId: string, newPassword: string): Promise<void> {
  await apiClient.post(`/users/${userId}/reset-password`, { newPassword });
}
