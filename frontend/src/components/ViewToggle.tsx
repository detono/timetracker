export type ViewMode = "list" | "planboard";

interface Props {
  value: ViewMode;
  onChange: (mode: ViewMode) => void;
}

export function ViewToggle({ value, onChange }: Props) {
  return (
    <div className="view-toggle" role="tablist" aria-label="View mode">
      <button
        role="tab"
        aria-selected={value === "list"}
        className={value === "list" ? "view-toggle__btn is-active" : "view-toggle__btn"}
        onClick={() => onChange("list")}
      >
        List
      </button>
      <button
        role="tab"
        aria-selected={value === "planboard"}
        className={value === "planboard" ? "view-toggle__btn is-active" : "view-toggle__btn"}
        onClick={() => onChange("planboard")}
      >
        Planboard
      </button>
    </div>
  );
}
