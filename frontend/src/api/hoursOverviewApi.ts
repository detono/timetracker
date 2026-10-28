import { apiClient } from "./client";
import type { MyHoursOverview, TeamMemberHours } from "../types";

/** `asOf` is the user's local date (yyyy-MM-dd), so "today" matches their time zone. */
export async function getMyHoursOverview(asOf: string): Promise<MyHoursOverview> {
  const { data } = await apiClient.get<MyHoursOverview>("/hours-overview/me", { params: { asOf } });
  return data;
}

/** Everyone for employers, their team for supervisors, nobody for other employees. */
export async function getTeamHoursOverview(asOf: string): Promise<TeamMemberHours[]> {
  const { data } = await apiClient.get<TeamMemberHours[]>("/hours-overview/team", { params: { asOf } });
  return data;
}
