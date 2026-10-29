import { apiClient } from "./client";
import type { Tenant, TenantEmployer } from "../types";

export interface CreateTenantPayload {
  name: string;
  employerFirstName: string;
  employerLastName: string;
  employerEmail: string;
  employerPassword: string;
}

export async function getTenants(): Promise<Tenant[]> {
  const { data } = await apiClient.get<Tenant[]>("/tenants");
  return data;
}

export async function createTenant(payload: CreateTenantPayload): Promise<Tenant> {
  const { data } = await apiClient.post<Tenant>("/tenants", payload);
  return data;
}

export async function setTenantActive(tenantId: string, isActive: boolean): Promise<void> {
  await apiClient.post(`/tenants/${tenantId}/${isActive ? "activate" : "deactivate"}`);
}

export async function getTenantEmployers(tenantId: string): Promise<TenantEmployer[]> {
  const { data } = await apiClient.get<TenantEmployer[]>(`/tenants/${tenantId}/employers`);
  return data;
}

/** Sets a temporary password; the employer must choose their own at their next login. */
export async function resetEmployerPassword(tenantId: string, userId: string, newPassword: string): Promise<void> {
  await apiClient.post(`/tenants/${tenantId}/employers/${userId}/reset-password`, { newPassword });
}
