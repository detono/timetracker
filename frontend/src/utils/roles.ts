import type { TFunction } from "i18next";
import type { UserRole } from "../types";

export function roleLabel(t: TFunction, role: UserRole): string {
  switch (role) {
    case "Employer":
      return t("roles.employer");
    case "PlatformAdmin":
      return t("roles.platformAdmin");
    default:
      return t("roles.employee");
  }
}
