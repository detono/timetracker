import { apiClient, API_BASE_URL, getAuthToken } from "./client";
import type { HoursBalanceLine, HoursReport, ReportGrouping } from "../types";

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

/** Worked vs. expected hours per person per period. */
export async function getHoursBalance(
  from: string,
  to: string,
  grouping: ReportGrouping,
  userId?: string
): Promise<HoursBalanceLine[]> {
  const { data } = await apiClient.get<HoursBalanceLine[]>("/reports/hours/balance", {
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
  await downloadCsv("hours/csv", `hours-report-${from}-${to}.csv`, from, to, grouping, userId);
}

/** The worked vs. expected table as a CSV file. */
export async function downloadHoursBalanceCsv(
  from: string,
  to: string,
  grouping: ReportGrouping,
  userId?: string
): Promise<void> {
  await downloadCsv("hours/balance/csv", `worked-vs-expected-${from}-${to}.csv`, from, to, grouping, userId);
}

async function downloadCsv(
  path: string,
  fileName: string,
  from: string,
  to: string,
  grouping: ReportGrouping,
  userId?: string
): Promise<void> {
  const params = new URLSearchParams({ from, to, grouping });
  if (userId) params.set("userId", userId);

  const response = await fetch(`${API_BASE_URL}/reports/${path}?${params.toString()}`, {
    headers: { Authorization: `Bearer ${getAuthToken() ?? ""}` }
  });

  if (!response.ok) {
    throw new Error("Failed to download CSV report.");
  }

  const blob = await response.blob();
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  link.click();
  window.URL.revokeObjectURL(url);
}
