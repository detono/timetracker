import { apiClient } from "./client";
import type { LeaveBalance, LeavePreview, LeaveRequest, LeaveRequestStatus, WorkSchedule, WorkScheduleDay } from "../types";

export type LeaveRequestScope = "Mine" | "ToReview";

export async function getLeaveRequests(scope: LeaveRequestScope, status?: LeaveRequestStatus): Promise<LeaveRequest[]> {
  const { data } = await apiClient.get<LeaveRequest[]>("/leave-requests", { params: { scope, status } });
  return data;
}

/** How many requests wait for the signed-in user's review. */
export async function getPendingLeaveCount(): Promise<number> {
  const { data } = await apiClient.get<number>("/leave-requests/pending-count");
  return data;
}

export async function previewLeaveRequest(hourTypeId: string, startDate: string, endDate: string): Promise<LeavePreview> {
  const { data } = await apiClient.get<LeavePreview>("/leave-requests/preview", {
    params: { hourTypeId, startDate, endDate }
  });
  return data;
}

export interface CreateLeaveRequestPayload {
  hourTypeId: string;
  startDate: string;
  endDate: string;
  comment: string | null;
}

export async function createLeaveRequest(payload: CreateLeaveRequestPayload): Promise<LeaveRequest> {
  const { data } = await apiClient.post<LeaveRequest>("/leave-requests", payload);
  return data;
}

export async function approveLeaveRequest(id: string, comment: string | null): Promise<LeaveRequest> {
  const { data } = await apiClient.post<LeaveRequest>(`/leave-requests/${id}/approve`, { comment });
  return data;
}

export async function rejectLeaveRequest(id: string, comment: string | null): Promise<LeaveRequest> {
  const { data } = await apiClient.post<LeaveRequest>(`/leave-requests/${id}/reject`, { comment });
  return data;
}

export async function cancelLeaveRequest(id: string, comment: string | null): Promise<LeaveRequest> {
  const { data } = await apiClient.post<LeaveRequest>(`/leave-requests/${id}/cancel`, { comment });
  return data;
}

export async function getLeaveBalances(year: number): Promise<LeaveBalance[]> {
  const { data } = await apiClient.get<LeaveBalance[]>("/leave-balances", { params: { year } });
  return data;
}

export async function setLeaveAllowance(userId: string, year: number, allowanceHours: number): Promise<void> {
  await apiClient.put(`/leave-balances/${userId}/${year}`, { allowanceHours });
}

/** Someone's schedule, or the company's when `userId` is null. */
export async function getWorkSchedule(userId: string | null): Promise<WorkSchedule> {
  const { data } = await apiClient.get<WorkSchedule>(`/work-schedules/${userId ?? "company"}`);
  return data;
}

/** Gives someone their own schedule, or sets the company's when `userId` is null. */
export async function updateWorkSchedule(userId: string | null, days: WorkScheduleDay[]): Promise<WorkSchedule> {
  const { data } = await apiClient.put<WorkSchedule>(`/work-schedules/${userId ?? "company"}`, days);
  return data;
}

/** Removes someone's own schedule so they follow the company's again. */
export async function resetWorkSchedule(userId: string): Promise<WorkSchedule> {
  const { data } = await apiClient.delete<WorkSchedule>(`/work-schedules/${userId}`);
  return data;
}
