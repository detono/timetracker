import { apiClient } from "./client";
import type { AuditEntry } from "../types";

/** Employer only. Newest first; pass the last entry's time as `before` for the next page. */
export async function getAuditLog(before?: string, take = 50): Promise<AuditEntry[]> {
  const { data } = await apiClient.get<AuditEntry[]>("/audit-log", { params: { before, take } });
  return data;
}
