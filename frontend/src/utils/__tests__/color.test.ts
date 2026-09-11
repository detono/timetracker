import { describe, expect, it } from "vitest";
import { darken } from "../color";

describe("darken", () => {
  it("scales each RGB channel down by the given amount", () => {
    expect(darken("#ffffff", 0.5)).toBe("#808080");
  });

  it("expands 3-digit hex codes before darkening", () => {
    expect(darken("#fff", 0)).toBe("#ffffff");
  });

  it("never goes below zero", () => {
    expect(darken("#000000", 0.5)).toBe("#000000");
  });

  it("returns the input unchanged if it isn't valid hex", () => {
    expect(darken("not-a-color", 0.5)).toBe("not-a-color");
  });
});
