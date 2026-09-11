import { apiClient } from "./client";
import type { AuthResult } from "../types";

export async function login(email: string, password: string): Promise<AuthResult> {
  const { data } = await apiClient.post<AuthResult>("/auth/login", { email, password });
  return data;
}

export async function register(
  firstName: string,
  lastName: string,
  email: string,
  password: string,
  role: "Employee" | "Employer"
) {
  const { data } = await apiClient.post("/auth/register", { firstName, lastName, email, password, role });
  return data;
}

export async function changePassword(currentPassword: string, newPassword: string): Promise<void> {
  await apiClient.post("/auth/change-password", { currentPassword, newPassword });
}