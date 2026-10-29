import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import i18n from "../../i18n";
import { HourTypeBadge } from "../HourTypeBadge";

describe("HourTypeBadge", () => {
  it("shows the name in the current language, else English, else 'Unknown'", async () => {
    await i18n.changeLanguage("nl");
    const { rerender } = render(<HourTypeBadge localizedNames={{ en: "Work", nl: "Werk" }} colorHex="#932e4a" />);
    expect(screen.getByText("Werk")).toBeInTheDocument();

    rerender(<HourTypeBadge localizedNames={{ en: "Work" }} colorHex="#932e4a" />);
    expect(screen.getByText("Work")).toBeInTheDocument();

    rerender(<HourTypeBadge colorHex="#932e4a" />);
    expect(screen.getByText("Unknown")).toBeInTheDocument();
    await i18n.changeLanguage("en");
  });

  it("prefers a ready-made name, e.g. from a report", () => {
    render(<HourTypeBadge name="Ziekte" localizedNames={{ en: "Sick leave" }} colorHex="#c0392b" />);
    expect(screen.getByText("Ziekte")).toBeInTheDocument();
  });
});
