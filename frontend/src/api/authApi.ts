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

/**
 * Changes the signed-in user's password and returns a fresh sign-in; other sessions are signed
 * out. The current password can be left out while a temporary password must be replaced.
 */
export async function changePassword(currentPassword: string | null, newPassword: string): Promise<AuthResult> {
  const { data } = await apiClient.post<AuthResult>("/auth/change-password", { currentPassword, newPassword });
  return data;
}

/** Whether "forgot password" can work: some company on this installation has email set up. */
export async function getPasswordResetAvailable(): Promise<boolean> {
  const { data } = await apiClient.get<{ available: boolean }>("/auth/password-reset");
  return data.available;
}

/** Emails a reset code if the account exists and its company has email set up. Always succeeds. */
export async function requestPasswordReset(email: string): Promise<void> {
  await apiClient.post("/auth/password-reset", { email });
}

export async function confirmPasswordReset(email: string, code: string, newPassword: string): Promise<void> {
  await apiClient.post("/auth/password-reset/confirm", { email, code, newPassword });
}
