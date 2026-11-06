import { describe, expect, it } from "vitest";
import { version } from "../../../package.json";
import { appVersion } from "../../version";

describe("appVersion", () => {
  it("is the version in package.json", () => {
    expect(appVersion).toBe(version);
    expect(appVersion).toMatch(/^\d+\.\d+\.\d+/);
  });
});
