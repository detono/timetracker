import { apiClient } from "./client";
import type { WhatsNew } from "../types";

/** The releases you haven't seen the notes of yet. */
export async function getWhatsNew(): Promise<WhatsNew> {
  const { data } = await apiClient.get<WhatsNew>("/whats-new");
  return data;
}

/** Remembers on your account that you saw the notes up to this version. */
export async function markWhatsNewSeen(version: string): Promise<void> {
  await apiClient.post("/whats-new/seen", { version });
}
