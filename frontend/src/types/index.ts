export type UserRole = "Employee" | "Employer" | "PlatformAdmin";

export interface AuthResult {
  token: string;
  userId: string;
  fullName: string;
  role: UserRole;
  /** The user's company; null for platform admins. */
  tenantName: string | null;
  /** The language the user picked; null if they haven't yet. */
  language: string | null;
}

export interface Tenant {
  id: string;
  name: string;
  isActive: boolean;
  userCount: number;
  createdAtUtc: string;
}

export interface UserAccount {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: UserRole;
  supervisorId: string | null;
  isActive: boolean;
  /** Whether their logged hours are compared with their work schedule. */
  tracksHours: boolean;
}

export interface HourType {
  id: string;
  localizedNames: Record<string, string>;
  colorHex: string;
  isActive: boolean;
  isDefault: boolean;
  /** Can be requested as leave. */
  isLeave: boolean;
  /** Uses up the yearly leave balance. */
  deductsFromLeaveBalance: boolean;
}

export interface TimeEntry {
  id: string;
  userId: string;
  userFullName: string;
  hourTypeId: string;
  projectId: string | null;
  localizedHourTypeNames: Record<string, string>;
  hourTypeColor: string;
  workDate: string; // yyyy-MM-dd
  startTime: string; // HH:mm:ss
  endTime: string;
  breakMinutes: number;
  durationHours: number;
  notes: string | null;
  projectName: string | null;
  location: string | null;
  latitude: number | null;
  longitude: number | null;
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
  localizedHourTypeNames: Record<string, string>;
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

export type MailProvider = "Smtp" | "MicrosoftGraph";
export type SmtpSecurity = "StartTls" | "SslOnConnect" | "None";

/** A company's mail settings as the API returns them: secrets are never included, only whether one is set. */
export interface MailSettings {
  isConfigured: boolean;
  isEnabled: boolean;
  provider: MailProvider;
  fromAddress: string;
  fromName: string | null;
  smtpHost: string | null;
  smtpPort: number;
  smtpSecurity: SmtpSecurity;
  smtpUsername: string | null;
  hasSmtpPassword: boolean;
  graphTenantId: string | null;
  graphClientId: string | null;
  graphMailbox: string | null;
  hasGraphClientSecret: boolean;
}

/** Body for saving mail settings. An empty or null secret keeps the stored one. */
export interface UpdateMailSettingsPayload {
  provider: MailProvider;
  isEnabled: boolean;
  fromAddress: string;
  fromName: string | null;
  smtpHost: string | null;
  smtpPort: number;
  smtpSecurity: SmtpSecurity;
  smtpUsername: string | null;
  smtpPassword: string | null;
  graphTenantId: string | null;
  graphClientId: string | null;
  graphClientSecret: string | null;
  graphMailbox: string | null;
}

export type Weekday = "Monday" | "Tuesday" | "Wednesday" | "Thursday" | "Friday" | "Saturday" | "Sunday";

export interface WorkScheduleDay {
  dayOfWeek: Weekday;
  startTime: string; // HH:mm[:ss]
  endTime: string;
  breakMinutes: number;
}

/** Where someone's hours come from: their own schedule, the company's, or the built-in Monday-Friday 9-17. */
export type WorkScheduleSource = "Personal" | "Company" | "BuiltIn";

export interface WorkSchedule {
  /** Null for the company schedule. */
  userId: string | null;
  source: WorkScheduleSource;
  days: WorkScheduleDay[];
  weeklyHours: number;
}

export type LeaveRequestStatus = "Pending" | "Approved" | "Rejected" | "Cancelled";

/** Which part of a day leave covers. Over several days, only the first day can be "Afternoon" and the last "Morning". */
export type DayPart = "Full" | "Morning" | "Afternoon";

export interface LeaveRequest {
  id: string;
  userId: string;
  userFullName: string;
  hourTypeId: string;
  localizedHourTypeNames: Record<string, string>;
  hourTypeColor: string;
  startDate: string; // yyyy-MM-dd
  endDate: string;
  firstDayPart: DayPart;
  lastDayPart: DayPart;
  hours: number;
  comment: string | null;
  status: LeaveRequestStatus;
  reviewedByName: string | null;
  reviewedAtUtc: string | null;
  reviewComment: string | null;
  createdAtUtc: string;
  canReview: boolean;
  canCancel: boolean;
  /** For pending requests that use up the balance: what the requester has left before it's booked. */
  remainingBalanceHours: number | null;
  exceedsBalance: boolean;
}

export interface LeavePreview {
  /** A half day counts as 0.5. */
  workingDays: number;
  hours: number;
  deductsFromBalance: boolean;
  remainingBalanceHours: number | null;
  exceedsBalance: boolean;
  overlapsExistingRequest: boolean;
}

export interface LeaveBalance {
  userId: string;
  userFullName: string;
  year: number;
  /** Null when no allowance was set for this year. */
  allowanceHours: number | null;
  usedHours: number;
  pendingHours: number;
  remainingHours: number | null;
}

/** Logged versus scheduled hours over a period ending today. Positive difference = more than scheduled. */
export interface HoursPeriod {
  from: string;
  to: string;
  workedHours: number;
  expectedHours: number;
  differenceHours: number;
}

export interface HoursDay {
  date: string;
  workedHours: number;
  expectedHours: number;
  /** Not counted in the totals yet: a day still to come, or today while nothing is logged for it. */
  isPending: boolean;
  hasLeave: boolean;
  /** The public holiday or company day off on this day, if any. */
  holidayName: string | null;
}

export interface MyHoursOverview {
  asOf: string;
  /** False for people whose hours aren't compared with a schedule; the overview is then hidden. */
  tracksHours: boolean;
  scheduleSource: WorkScheduleSource;
  weeklyScheduleHours: number;
  week: HoursPeriod;
  month: HoursPeriod;
  year: HoursPeriod;
  days: HoursDay[];
}

export interface TeamMemberHours {
  userId: string;
  fullName: string;
  scheduleSource: WorkScheduleSource;
  weeklyScheduleHours: number;
  week: HoursPeriod;
  month: HoursPeriod;
  year: HoursPeriod;
}

export interface Company {
  name: string;
  street: string | null;
  postalCode: string | null;
  city: string | null;
  /** ISO 3166-1 alpha-2, e.g. "BE". Decides which public holidays apply. */
  countryCode: string | null;
  /** ISO 3166-2, e.g. "DE-BY", for countries where holidays differ per region. */
  region: string | null;
}

export interface PublicHoliday {
  id: string;
  date: string; // yyyy-MM-dd
  name: string;
}

export interface PublicHolidayImport {
  added: number;
  holidays: PublicHoliday[];
}
