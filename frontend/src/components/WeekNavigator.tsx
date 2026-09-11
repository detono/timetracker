import { useTranslation } from "react-i18next";
import { addWeeks } from "date-fns";
import { formatDayLabel } from "../utils/dateRange";

interface Props {
  anchorDate: Date;
  onChange: (date: Date) => void;
  from: string;
  to: string;
}

export function WeekNavigator({ anchorDate, onChange, from, to }: Props) {
  const { t } = useTranslation();

  return (
    <div className="week-nav">
      <button className="btn btn--ghost btn--sm" onClick={() => onChange(addWeeks(anchorDate, -1))}>
        ← {t('weekNavigator.prevWeek')}
      </button>
      <span className="week-nav__range">
        {formatDayLabel(from)} – {formatDayLabel(to)}
      </span>
      <button className="btn btn--ghost btn--sm" onClick={() => onChange(addWeeks(anchorDate, 1))}>
        {t('weekNavigator.nextWeek')} →
      </button>
      <button className="btn btn--ghost btn--sm" onClick={() => onChange(new Date())}>
        {t('weekNavigator.today')}
      </button>
    </div>
  );
}