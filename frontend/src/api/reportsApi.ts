import { apiClient, API_BASE_URL, getAuthToken } from "./client";
import type { HoursReport, ReportGrouping } from "../types";

export async function getHoursReport(
  from: string,
  to: string,
  grouping: ReportGrouping,
  userId?: string
): Promise<HoursReport> {
  const { data } = await apiClient.get<HoursReport>("/reports/hours", {
    params: { from, to, grouping, userId }
  });
  return data;
}

/** Builds a direct download URL for the CSV extract, including the bearer token as a fetch call (not a plain link, since the API requires auth). */
export async function downloadHoursReportCsv(
  from: string,
  to: string,
  grouping: ReportGrouping,
  userId?: string
): Promise<void> {
  const params = new URLSearchParams({ from, to, grouping });
  if (userId) params.set("userId", userId);

  const response = await fetch(`${API_BASE_URL}/reports/hours/csv?${params.toString()}`, {
    headers: { Authorization: `Bearer ${getAuthToken() ?? ""}` }
  });

  if (!response.ok) {
    throw new Error("Failed to download CSV report.");
  }

  const blob = await response.blob();
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = `hours-report-${from}-${to}.csv`;
  link.click();
  window.URL.revokeObjectURL(url);
}
