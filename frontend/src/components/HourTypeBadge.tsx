import type { CSSProperties } from 'react';
import { useTranslation } from 'react-i18next';

interface HourTypeBadgeProps {
  /** A name that's already in the right language, e.g. from a report. Wins over localizedNames. */
  name?: string;
  /** The name per language; the current language is shown, else English. */
  localizedNames?: Record<string, string>;
  colorHex: string;
}

/**
 * An hour type as a small pill: a dot in its color and a light tint of it behind the name. The
 * text keeps the normal ink color, so it stays readable in light and dark mode whatever color
 * a company picks.
 */
export const HourTypeBadge = ({ name, localizedNames, colorHex }: HourTypeBadgeProps) => {
  const { i18n } = useTranslation();
  const language = i18n.language.split('-')[0];

  // Fallback chain: given name -> current language -> English -> Unknown
  const displayName = name
    || localizedNames?.[language]
    || localizedNames?.['en']
    || 'Unknown';

  return (
    <span className="hour-type-badge" style={{ '--badge-color': colorHex } as CSSProperties}>
      <span className="hour-type-badge__dot" aria-hidden="true" />
      {displayName}
    </span>
  );
};
