import { apiClient } from "./client";
import type { MailSettings, UpdateMailSettingsPayload } from "../types";

export async function getMailSettings(): Promise<MailSettings> {
  const { data } = await apiClient.get<MailSettings>("/mail-settings");
  return data;
}

export async function updateMailSettings(payload: UpdateMailSettingsPayload): Promise<MailSettings> {
  const { data } = await apiClient.put<MailSettings>("/mail-settings", payload);
  return data;
}

/** Sends a test email to the signed-in employer. Rejects with the delivery error on failure. */
export async function sendTestEmail(): Promise<void> {
  await apiClient.post("/mail-settings/test");
}
