#!/usr/bin/env node
// Lists, per language, the texts that are still missing compared with English, for both
// the app (frontend/src/locales) and the emails (src/TimeTracker.Application/Resources/Emails).
// Missing texts fall back to English, so this is a to-do list for translators, not an error.
import { readdirSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { baseKey, flatten } from "./i18n-keys.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const sets = {
  app: join(here, "../src/locales"),
  emails: join(here, "../../src/TimeTracker.Application/Resources/Emails")
};

let total = 0;
for (const [name, dir] of Object.entries(sets)) {
  const read = (file) => flatten(JSON.parse(readFileSync(join(dir, file), "utf8")));
  const english = new Set(Object.keys(read("en.json")).map(baseKey));

  for (const file of readdirSync(dir).filter((f) => f.endsWith(".json") && f !== "en.json").sort()) {
    const have = new Set(Object.keys(read(file)).map(baseKey));
    const missing = [...english].filter((key) => !have.has(key));
    total += missing.length;
    console.log(`${name} ${file.replace(".json", "")}: ${missing.length === 0 ? "complete" : `${missing.length} missing`}`);
    missing.forEach((key) => console.log(`  - ${key}`));
  }
}

console.log(total === 0 ? "\nAll languages are complete." : `\n${total} text(s) still fall back to English.`);
