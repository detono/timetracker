import type { MailProvider, MailSettings, SmtpSecurity, UpdateMailSettingsPayload } from "../types";

/** Editable form state. Secrets start empty: leaving them empty keeps the stored value. */
export interface MailSettingsForm {
  provider: MailProvider;
  isEnabled: boolean;
  fromAddress: string;
  fromName: string;
  smtpHost: string;
  smtpPort: number;
  smtpSecurity: SmtpSecurity;
  smtpUsername: string;
  smtpPassword: string;
  graphTenantId: string;
  graphClientId: string;
  graphClientSecret: string;
  graphMailbox: string;
}

/** The usual port for each SMTP security mode. */
export function defaultSmtpPort(security: SmtpSecurity): number {
  switch (security) {
    case "SslOnConnect":
      return 465;
    case "None":
      return 25;
    default:
      return 587;
  }
}

export function formFromSettings(settings: MailSettings): MailSettingsForm {
  return {
    provider: settings.provider,
    isEnabled: settings.isConfigured ? settings.isEnabled : true,
    fromAddress: settings.fromAddress,
    fromName: settings.fromName ?? "",
    smtpHost: settings.smtpHost ?? "",
    smtpPort: settings.smtpPort,
    smtpSecurity: settings.smtpSecurity,
    smtpUsername: settings.smtpUsername ?? "",
    smtpPassword: "",
    graphTenantId: settings.graphTenantId ?? "",
    graphClientId: settings.graphClientId ?? "",
    graphClientSecret: "",
    graphMailbox: settings.graphMailbox ?? ""
  };
}

const orNull = (value: string) => (value.trim() === "" ? null : value.trim());

/** Builds the save request. Only the chosen provider's secret is sent; an empty one keeps the stored secret. */
export function toUpdatePayload(form: MailSettingsForm): UpdateMailSettingsPayload {
  const isSmtp = form.provider === "Smtp";
  return {
    provider: form.provider,
    isEnabled: form.isEnabled,
    fromAddress: form.fromAddress.trim(),
    fromName: orNull(form.fromName),
    smtpHost: orNull(form.smtpHost),
    smtpPort: form.smtpPort,
    smtpSecurity: form.smtpSecurity,
    smtpUsername: orNull(form.smtpUsername),
    // Secrets aren't trimmed: spaces can be part of a real password.
    smtpPassword: isSmtp && form.smtpPassword !== "" ? form.smtpPassword : null,
    graphTenantId: orNull(form.graphTenantId),
    graphClientId: orNull(form.graphClientId),
    graphClientSecret: !isSmtp && form.graphClientSecret !== "" ? form.graphClientSecret : null,
    graphMailbox: orNull(form.graphMailbox)
  };
}
