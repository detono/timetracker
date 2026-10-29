import { describe, expect, it } from "vitest";
import i18n from "../../i18n";
import { roleLabel } from "../roles";
import { nativeLanguageName } from "../languages";
import { dateLocale } from "../dateRange";

describe("roleLabel", () => {
  it("names every role in the current language", async () => {
    await i18n.changeLanguage("en");
    expect(roleLabel(i18n.t, "Employer")).toBe(i18n.t("roles.employer"));
    expect(roleLabel(i18n.t, "PlatformAdmin")).toBe(i18n.t("roles.platformAdmin"));
    expect(roleLabel(i18n.t, "Employee")).toBe(i18n.t("roles.employee"));
    expect(new Set([roleLabel(i18n.t, "Employer"), roleLabel(i18n.t, "Employee")]).size).toBe(2);
  });
});

describe("nativeLanguageName", () => {
  it("names a language in that language, capitalised", () => {
    expect(nativeLanguageName("nl")).toBe("Nederlands");
    expect(nativeLanguageName("fr")).toBe("Français");
  });

  it("falls back to the code for something that isn't a language", () => {
    expect(nativeLanguageName("not a language")).toBe("not a language");
  });
});

describe("dateLocale", () => {
  it("uses British English for English, the language itself otherwise", () => {
    expect(dateLocale("en")).toBe("en-GB");
    expect(dateLocale("nl")).toBe("nl");
  });
});
