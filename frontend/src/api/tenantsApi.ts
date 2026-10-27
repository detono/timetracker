import { apiClient } from "./client";
import type { Tenant } from "../types";

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
