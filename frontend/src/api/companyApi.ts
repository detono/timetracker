import { apiClient } from "./client";
import type { Company, PublicHoliday, PublicHolidayImport } from "../types";

export async function getCompany(): Promise<Company> {
  const { data } = await apiClient.get<Company>("/company");
  return data;
}

export async function updateCompany(company: Company): Promise<Company> {
  const { data } = await apiClient.put<Company>("/company", company);
  return data;
}

export async function getPublicHolidays(year: number): Promise<PublicHoliday[]> {
  const { data } = await apiClient.get<PublicHoliday[]>("/public-holidays", { params: { year } });
  return data;
}

export async function addPublicHoliday(date: string, name: string): Promise<PublicHoliday> {
  const { data } = await apiClient.post<PublicHoliday>("/public-holidays", { date, name });
  return data;
}

export async function deletePublicHoliday(id: string): Promise<void> {
  await apiClient.delete(`/public-holidays/${id}`);
}

/** Adds the official holidays of the company's country (and region) for a year. */
export async function importPublicHolidays(year: number): Promise<PublicHolidayImport> {
  const { data } = await apiClient.post<PublicHolidayImport>("/public-holidays/import", null, { params: { year } });
  return data;
}
