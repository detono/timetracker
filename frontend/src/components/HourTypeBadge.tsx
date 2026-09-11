interface Props {
  name: string;
  colorHex: string;
}

/** Renders an hour type as a small colored pill, using its own color for a left accent bar. */
export function HourTypeBadge({ name, colorHex }: Props) {
  return (
    <span
      className="hour-type-badge"
      style={{ borderLeftColor: colorHex, backgroundColor: `${colorHex}22` }}
    >
      {name}
    </span>
  );
}
