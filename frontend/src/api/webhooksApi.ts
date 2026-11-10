import { apiClient } from "./client";
import type { Webhook, WebhookDelivery, WebhooksOverview } from "../types";

export interface WebhookPayload {
  name: string;
  url: string;
  events: string[];
  isEnabled: boolean;
  /** Empty sends no extra header. */
  headerName: string | null;
  /** Empty keeps the stored value. */
  headerValue: string | null;
}

export async function getWebhooks(): Promise<WebhooksOverview> {
  const { data } = await apiClient.get<WebhooksOverview>("/webhooks");
  return data;
}

export async function createWebhook(payload: WebhookPayload): Promise<Webhook> {
  const { data } = await apiClient.post<Webhook>("/webhooks", payload);
  return data;
}

export async function updateWebhook(id: string, payload: WebhookPayload): Promise<Webhook> {
  const { data } = await apiClient.put<Webhook>(`/webhooks/${id}`, payload);
  return data;
}

export async function deleteWebhook(id: string): Promise<void> {
  await apiClient.delete(`/webhooks/${id}`);
}

export async function getWebhookSecret(id: string): Promise<string> {
  const { data } = await apiClient.get<{ secret: string }>(`/webhooks/${id}/secret`);
  return data.secret;
}

/** The old secret stops working right away. */
export async function rotateWebhookSecret(id: string): Promise<string> {
  const { data } = await apiClient.post<{ secret: string }>(`/webhooks/${id}/secret`);
  return data.secret;
}

/** Sends a made-up event right away; the result says how it went. */
export async function testWebhook(id: string): Promise<WebhookDelivery> {
  const { data } = await apiClient.post<WebhookDelivery>(`/webhooks/${id}/test`);
  return data;
}

export async function retryWebhookDelivery(id: string): Promise<void> {
  await apiClient.post(`/webhooks/deliveries/${id}/retry`);
}
