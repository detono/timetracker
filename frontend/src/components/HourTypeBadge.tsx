import { useTranslation } from 'react-i18next';

interface HourTypeBadgeProps {
  /** A name that's already in the right language, e.g. from a report. Wins over localizedNames. */
  name?: string;
  /** The name per language; the current language is shown, else English. */
  localizedNames?: Record<string, string>;
  colorHex: string;
}

export const HourTypeBadge = ({ name, localizedNames, colorHex }: HourTypeBadgeProps) => {
  const { i18n } = useTranslation();

  // Fallback chain: given name -> current language -> English -> Unknown
  const displayName = name
    || localizedNames?.[i18n.language]
    || localizedNames?.['en']
    || 'Unknown';

  return (
    <span style={{ backgroundColor: colorHex }} className="...">
      {displayName}
    </span>
  );
};
