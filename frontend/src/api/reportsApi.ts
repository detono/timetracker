import { apiClient, API_BASE_URL, getAuthToken } from "./client";
import type { HoursBalanceLine, HoursReport, ProjectHoursLine, ReportGrouping } from "../types";

/** Hours per person, hour type and period; with a projectId, only the hours on that project. */
export async function getHoursReport(
  from: string,
  to: string,
  grouping: ReportGrouping,
  userId?: string,
  projectId?: string
): Promise<HoursReport> {
  const { data } = await apiClient.get<HoursReport>("/reports/hours", {
    params: { from, to, grouping, userId, projectId }
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
  userId?: string,
  projectId?: string
): Promise<void> {
  await downloadCsv("hours/csv", `hours-report-${from}-${to}.csv`, { from, to, grouping, userId, projectId });
}

/** The worked vs. expected table as a CSV file. */
export async function downloadHoursBalanceCsv(
  from: string,
  to: string,
  grouping: ReportGrouping,
  userId?: string
): Promise<void> {
  await downloadCsv("hours/balance/csv", `worked-vs-expected-${from}-${to}.csv`, { from, to, grouping, userId });
}

/** Every entry in the range, one row each (date, start, end, break, type, project, ...), as a CSV file. */
export async function downloadTimeEntriesCsv(
  from: string,
  to: string,
  userId?: string,
  projectId?: string
): Promise<void> {
  await downloadCsv("entries/csv", `time-entries-${from}-${to}.csv`, { from, to, userId, projectId });
}

/** Hours per project in the dates (and a line for hours without a project). */
export async function getProjectHours(from: string, to: string, userId?: string): Promise<ProjectHoursLine[]> {
  const { data } = await apiClient.get<ProjectHoursLine[]>("/reports/projects", { params: { from, to, userId } });
  return data;
}

/** The hours per project as a CSV file. */
export async function downloadProjectHoursCsv(from: string, to: string, userId?: string): Promise<void> {
  await downloadCsv("projects/csv", `hours-per-project-${from}-${to}.csv`, { from, to, userId });
}

async function downloadCsv(
  path: string,
  fileName: string,
  query: Record<string, string | undefined>
): Promise<void> {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(query)) {
    if (value) params.set(key, value);
  }

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
