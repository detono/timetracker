import { apiClient } from "./client";
import type { AuthResult } from "../types";

export async function login(email: string, password: string): Promise<AuthResult> {
  const { data } = await apiClient.post<AuthResult>("/auth/login", { email, password });
  return data;
}

/** Saves the signed-in user's language (two letters), used for the app and their emails. */
export async function setLanguage(language: string): Promise<void> {
  await apiClient.put("/auth/language", { language });
}

export async function changePassword(currentPassword: string, newPassword: string): Promise<void> {
  await apiClient.post("/auth/change-password", { currentPassword, newPassword });
}
