// Shared helpers for checking translation files against English.

/** Flattens nested translation objects into "a.b.c" keys. */
export function flatten(obj, prefix = "") {
  return Object.entries(obj).flatMap(([key, value]) =>
    value !== null && typeof value === "object"
      ? Object.entries(flatten(value, `${prefix}${key}.`))
      : [[`${prefix}${key}`, String(value)]]
  ).reduce((acc, [k, v]) => ((acc[k] = v), acc), {});
}

/** i18next plural keys ("x_one", "x_other") belong to one key "x". */
export function baseKey(key) {
  return key.replace(/_(zero|one|two|few|many|other)$/, "");
}

/** The placeholders in a text: {{name}} for the app, {name} for emails. */
export function placeholders(text) {
  return [...text.matchAll(/\{\{?\s*(\w+)\s*\}?\}/g)].map((m) => m[1]).sort();
}
