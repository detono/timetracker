import { useTranslation } from 'react-i18next';

interface HourTypeBadgeProps {
  localizedNames: Record<string, string>; // <-- Replaced 'name'
  colorHex: string;
}

export const HourTypeBadge = ({ localizedNames, colorHex }: HourTypeBadgeProps) => {
  const { i18n } = useTranslation();

  // Fallback chain: Current Locale -> English -> Unknown
  const displayName = localizedNames[i18n.language]
    || localizedNames['en']
    || 'Unknown';

  return (
    <span style={{ backgroundColor: colorHex }} className="...">
      {displayName}
    </span>
  );
};