import { describe, expect, it } from "vitest";
// @ts-expect-error plain JS helper shared with the i18n:missing script
import { baseKey, flatten, placeholders } from "../../../scripts/i18n-keys.mjs";

// Translations may be incomplete (missing texts fall back to English; see `npm run i18n:missing`),
// but what's there must match English: no unknown keys, and the same placeholders.
type Files = Record<string, Record<string, unknown>>;
const sets: Record<string, Files> = {
  app: import.meta.glob<Record<string, unknown>>("../../locales/*.json", { eager: true, import: "default" }),
  emails: import.meta.glob<Record<string, unknown>>(
    "../../../../src/TimeTracker.Application/Resources/Emails/*.json",
    { eager: true, import: "default" }
  )
};

const fileName = (path: string) => path.split("/").pop() ?? path;

for (const [name, files] of Object.entries(sets)) {
  const byName = Object.fromEntries(Object.entries(files).map(([path, content]) => [fileName(path), content]));
  const read = (file: string): Record<string, string> => flatten(byName[file]);
  const english = read("en.json");
  const englishBase = new Set(Object.keys(english).map(baseKey));
  const others = Object.keys(byName).filter((f) => f !== "en.json");

  describe(`${name} translations`, () => {
    it("are named by a two-letter language code", () => {
      expect(Object.keys(byName).length).toBeGreaterThan(0);
      Object.keys(byName).forEach((file) => expect(file).toMatch(/^[a-z]{2}\.json$/));
    });

    for (const file of others) {
      it(`${file} only uses keys that exist in English`, () => {
        const unknown = Object.keys(read(file)).filter((key) => !englishBase.has(baseKey(key)));
        expect(unknown).toEqual([]);
      });

      it(`${file} keeps the same placeholders as English`, () => {
        const translated = read(file);
        const mismatches = Object.entries(translated)
          .filter(([key]) => key in english)
          .filter(([key, text]) => placeholders(text).join() !== placeholders(english[key]).join())
          .map(([key]) => key);
        expect(mismatches).toEqual([]);
      });
    }
  });
}
