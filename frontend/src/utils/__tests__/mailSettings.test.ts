import { describe, expect, it } from "vitest";
import type { MailSettings } from "../../types";
import { defaultSmtpPort, formFromSettings, toUpdatePayload } from "../mailSettings";

const notConfigured: MailSettings = {
  isConfigured: false,
  isEnabled: false,
  provider: "Smtp",
  fromAddress: "",
  fromName: null,
  smtpHost: null,
  smtpPort: 587,
  smtpSecurity: "StartTls",
  smtpUsername: null,
  hasSmtpPassword: false,
  graphTenantId: null,
  graphClientId: null,
  graphMailbox: null,
  hasGraphClientSecret: false
};

describe("defaultSmtpPort", () => {
  it("matches the usual port for each security mode", () => {
    expect(defaultSmtpPort("StartTls")).toBe(587);
    expect(defaultSmtpPort("SslOnConnect")).toBe(465);
    expect(defaultSmtpPort("None")).toBe(25);
  });
});

describe("formFromSettings", () => {
  it("starts new settings enabled, with empty secrets", () => {
    const form = formFromSettings(notConfigured);
    expect(form.isEnabled).toBe(true);
    expect(form.smtpPassword).toBe("");
    expect(form.graphClientSecret).toBe("");
  });

  it("keeps the saved enabled flag once configured", () => {
    expect(formFromSettings({ ...notConfigured, isConfigured: true, isEnabled: false }).isEnabled).toBe(false);
  });
});

describe("toUpdatePayload", () => {
  const form = {
    ...formFromSettings(notConfigured),
    fromAddress: "  hr@acme.test ",
    fromName: " ",
    smtpHost: "smtp-relay.brevo.com",
    smtpUsername: "user",
    smtpPassword: " pass ",
    graphClientSecret: "should-not-be-sent"
  };

  it("sends only the chosen provider's secret, untrimmed", () => {
    const payload = toUpdatePayload(form);
    expect(payload.smtpPassword).toBe(" pass ");
    expect(payload.graphClientSecret).toBeNull();
  });

  it("turns blank fields into null and trims the rest", () => {
    const payload = toUpdatePayload(form);
    expect(payload.fromAddress).toBe("hr@acme.test");
    expect(payload.fromName).toBeNull();
    expect(payload.graphMailbox).toBeNull();
  });

  it("sends null for an empty secret so the stored one is kept", () => {
    const payload = toUpdatePayload({ ...form, provider: "MicrosoftGraph", graphClientSecret: "" });
    expect(payload.graphClientSecret).toBeNull();
    expect(payload.smtpPassword).toBeNull();
  });
});
