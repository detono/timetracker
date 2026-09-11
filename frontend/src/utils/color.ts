/** Returns a darker shade of a hex color by scaling each RGB channel down by `amount` (0-1). */
export function darken(hex: string, amount: number): string {
  const normalized = hex.replace("#", "");
  const expanded =
    normalized.length === 3
      ? normalized
          .split("")
          .map((c) => c + c)
          .join("")
      : normalized;

  const value = parseInt(expanded, 16);
  if (Number.isNaN(value)) {
    return hex;
  }

  const r = Math.max(0, Math.round(((value >> 16) & 255) * (1 - amount)));
  const g = Math.max(0, Math.round(((value >> 8) & 255) * (1 - amount)));
  const b = Math.max(0, Math.round((value & 255) * (1 - amount)));

  return `#${[r, g, b].map((c) => c.toString(16).padStart(2, "0")).join("")}`;
}
