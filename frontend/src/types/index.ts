export type UserRole = "Employee" | "Employer";

export interface AuthResult {
  token: string;
  userId: string;
  fullName: string;
  role: UserRole;
}

export interface UserAccount {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: UserRole;
  supervisorId: string | null;
  isActive: boolean;
}

export interface HourType {
  id: string;
  name: string;
  colorHex: string;
  isActive: boolean;
}

export interface TimeEntry {
  id: string;
  userId: string;
  userFullName: string;
  hourTypeId: string;
  projectId: string | null;
  hourTypeName: string;
  hourTypeColor: string;
  workDate: string; // yyyy-MM-dd
  startTime: string; // HH:mm:ss
  endTime: string;
  breakMinutes: number;
  durationHours: number;
  notes: string | null;
  projectName: string | null;
}

export interface Project {
  id: string;
  name: string;
  clientName: string | null;
  isActive: boolean;
  totalHoursLifetime: number;
  totalHoursThisMonth: number;
}
export interface ProjectBreakdown {
  userId: string;
  employeeName: string;
  totalHours: number;
}

export type ReportGrouping = "Day" | "Week" | "Month";

export interface HoursReportLine {
  userId: string;
  userFullName: string;
  hourTypeId: string;
  projectId?: string;
  hourTypeName: string;
  hourTypeColor: string;
  periodLabel: string;
  periodStart: string;
  periodEnd: string;
  totalHours: number;
  entryCount: number;
}

export interface HoursReport {
  grouping: ReportGrouping;
  from: string;
  to: string;
  lines: HoursReportLine[];
  grandTotalHours: number;
}
