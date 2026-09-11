import { addWeeks } from "date-fns";
import { formatDayLabel } from "../utils/dateRange";

interface Props {
  anchorDate: Date;
  onChange: (date: Date) => void;
  from: string;
  to: string;
}

export function WeekNavigator({ anchorDate, onChange, from, to }: Props) {
  return (
    <div className="week-nav">
      <button className="btn btn--ghost btn--sm" onClick={() => onChange(addWeeks(anchorDate, -1))}>
        ← Previous week
      </button>
      <span className="week-nav__range">
        {formatDayLabel(from)} – {formatDayLabel(to)}
      </span>
      <button className="btn btn--ghost btn--sm" onClick={() => onChange(addWeeks(anchorDate, 1))}>
        Next week →
      </button>
      <button className="btn btn--ghost btn--sm" onClick={() => onChange(new Date())}>
        Today
      </button>
    </div>
  );
}
