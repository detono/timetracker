import { beforeEach, describe, expect, it, vi } from "vitest";
import { act, fireEvent, render, screen, within } from "@testing-library/react";
import "../../i18n";
import { WebhooksPanel } from "../WebhooksPanel";
import { ConfirmProvider } from "../ConfirmDialog";
import { createWebhook, getWebhooks, retryWebhookDelivery, testWebhook } from "../../api/webhooksApi";
import type { Webhook, WebhooksOverview } from "../../types";

vi.mock("../../api/webhooksApi", () => ({
  getWebhooks: vi.fn(),
  createWebhook: vi.fn(),
  updateWebhook: vi.fn(),
  deleteWebhook: vi.fn(),
  getWebhookSecret: vi.fn().mockResolvedValue("whsec_abc"),
  rotateWebhookSecret: vi.fn(),
  testWebhook: vi.fn(),
  retryWebhookDelivery: vi.fn().mockResolvedValue(undefined)
}));

const events = ["leave.requested", "leave.approved", "leave.rejected", "leave.cancelled"];

const payroll: Webhook = {
  id: "w1", name: "Payroll", url: "https://hooks.example.com/leave", events: ["leave.approved"], isEnabled: true,
  headerName: "Authorization", hasHeaderValue: true, createdAtUtc: "2026-11-09T09:00:00Z"
};

const overview: WebhooksOverview = {
  availableEvents: events,
  webhooks: [payroll],
  recentDeliveries: [
    {
      id: "d1", webhookId: "w1", webhookName: "Payroll", eventType: "leave.approved",
      summary: "Jonathan Peeters · 2026-11-16 – 2026-11-20", status: "Failed", attempts: 6, lastStatusCode: null,
      lastError: "The address didn't answer in time.", createdAtUtc: "2026-11-09T10:15:00Z",
      lastAttemptAtUtc: "2026-11-09T16:15:00Z", nextAttemptAtUtc: null
    },
    {
      id: "d2", webhookId: "w1", webhookName: "Payroll", eventType: "leave.approved",
      summary: "Eva Maes · 2026-12-03", status: "Pending", attempts: 2, lastStatusCode: 503,
      lastError: "The address answered 503 Service Unavailable", createdAtUtc: "2026-11-09T11:00:00Z",
      lastAttemptAtUtc: "2026-11-09T11:01:00Z", nextAttemptAtUtc: "2026-11-09T11:06:00Z"
    }
  ]
};

function renderPanel() {
  render(
    <ConfirmProvider>
      <WebhooksPanel />
    </ConfirmProvider>
  );
}

describe("WebhooksPanel", () => {
  beforeEach(() => {
    vi.mocked(getWebhooks).mockReset().mockResolvedValue(overview);
    vi.mocked(createWebhook).mockReset();
    vi.mocked(retryWebhookDelivery).mockClear();
  });

  it("lists webhooks and recent calls, and sends a failed call again", async () => {
    renderPanel();

    expect(await screen.findByText("https://hooks.example.com/leave")).toBeInTheDocument();
    expect(screen.getAllByText("Leave approved").length).toBeGreaterThan(0);
    expect(screen.getByText("The address didn't answer in time.")).toBeInTheDocument();
    expect(screen.getByText(/Tried 2 times\. Next try/)).toBeInTheDocument();

    // Only the failed call offers "Send again".
    const again = screen.getAllByRole("button", { name: "Send again" });
    expect(again).toHaveLength(1);
    await act(async () => fireEvent.click(again[0]));
    expect(retryWebhookDelivery).toHaveBeenCalledWith("d1");
  });

  it("needs at least one event, then creates the webhook and keeps it open", async () => {
    const created: Webhook = { ...payroll, id: "w2", name: "HR", url: "https://hr.example.com/hook", events: ["leave.requested", "leave.cancelled"], headerName: null, hasHeaderValue: false };
    vi.mocked(createWebhook).mockResolvedValue(created);
    renderPanel();
    fireEvent.click(await screen.findByRole("button", { name: "Add webhook" }));

    fireEvent.change(screen.getByLabelText("Name"), { target: { value: "HR" } });
    fireEvent.change(screen.getByLabelText("URL"), { target: { value: " https://hr.example.com/hook " } });
    await act(async () => fireEvent.click(screen.getByRole("button", { name: "Save" })));
    expect(screen.getByText("Choose at least one event.")).toBeInTheDocument();
    expect(createWebhook).not.toHaveBeenCalled();

    const form = screen.getByRole("heading", { name: "Add webhook" }).closest("form")!;
    fireEvent.click(within(form).getByLabelText("Leave cancelled"));
    fireEvent.click(within(form).getByLabelText("Leave requested"));
    await act(async () => fireEvent.click(screen.getByRole("button", { name: "Save" })));

    expect(createWebhook).toHaveBeenCalledWith({
      name: "HR", url: "https://hr.example.com/hook", events: ["leave.requested", "leave.cancelled"],
      isEnabled: true, headerName: null, headerValue: null
    });
    expect(await screen.findByRole("heading", { name: "Edit HR" })).toBeInTheDocument();
    expect(screen.getByText(/Webhook added/)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Send test" })).toBeInTheDocument();
  });

  it("shows how a test call went", async () => {
    vi.mocked(testWebhook).mockResolvedValue({ ...overview.recentDeliveries[0], eventType: "webhook.test", status: "Failed", lastError: "The address answered 401 Unauthorized" });
    renderPanel();
    fireEvent.click(await screen.findByRole("button", { name: "Actions: Payroll" }));
    fireEvent.click(screen.getByRole("menuitem", { name: "Edit" }));

    await act(async () => fireEvent.click(screen.getByRole("button", { name: "Send test" })));

    expect(screen.getByText("Test failed: The address answered 401 Unauthorized")).toBeInTheDocument();
  });
});
