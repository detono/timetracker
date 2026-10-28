import { describe, expect, it } from "vitest";
import { diffTone, signedHm } from "../hoursOverview";

describe("diffTone", () => {
  it("treats a few minutes either way as on target", () => {
    expect(diffTone(0.1, 38.5)).toBe("even");
    expect(diffTone(-0.1, 38.5)).toBe("even");
  });

  it("separates a little short from more than a working day behind", () => {
    expect(diffTone(2, 38.5)).toBe("plus");
    expect(diffTone(-2, 38.5)).toBe("short");
    expect(diffTone(-7.7, 38.5)).toBe("behind");
    expect(diffTone(-4, 20)).toBe("behind");
  });
});

describe("signedHm", () => {
  it("adds a sign and drops it for zero", () => {
    expect(signedHm(1.5)).toBe("+1h 30m");
    expect(signedHm(-2)).toBe("−2h");
    expect(signedHm(0)).toBe("0h");
    expect(signedHm(0.5)).toBe("+30m");
    expect(signedHm(-0.25)).toBe("−15m");
  });
});
