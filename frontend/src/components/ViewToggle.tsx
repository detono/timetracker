import { useTranslation } from "react-i18next";

export type ViewMode = "list" | "planboard";

interface Props {
  value: ViewMode;
  onChange: (mode: ViewMode) => void;
}

export function ViewToggle({ value, onChange }: Props) {
  const { t } = useTranslation();
  return (
    <div className="view-toggle" role="tablist" aria-label={t('nav.viewMode')}>
      <button
        role="tab"
        aria-selected={value === "list"}
        className={value === "list" ? "view-toggle__btn is-active" : "view-toggle__btn"}
        onClick={() => onChange("list")}
      >
        {t('nav.list')}
      </button>
      <button
        role="tab"
        aria-selected={value === "planboard"}
        className={value === "planboard" ? "view-toggle__btn is-active" : "view-toggle__btn"}
        onClick={() => onChange("planboard")}
      >
        {t('nav.planboard')}
      </button>
    </div>
  );
}
