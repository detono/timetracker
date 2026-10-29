import { useEffect, useId, useLayoutEffect, useRef, useState, type KeyboardEvent } from "react";
import { useTranslation } from "react-i18next";
import { Icon } from "./Icon";

export interface RowMenuItem {
  label: string;
  onSelect: () => void;
  /** Shown in red, for actions like delete or deactivate. */
  danger?: boolean;
}

interface Props {
  items: RowMenuItem[];
  /** What the row is about, for screen readers: "Actions for Jane Doe". */
  label?: string;
  disabled?: boolean;
}

/**
 * The "⋯" button at the end of a table row with the row's actions in a small menu. The menu
 * is positioned against the window, so a table that scrolls sideways doesn't cut it off.
 * Closes on a choice, a click elsewhere, Escape, scrolling or resizing.
 */
export function RowMenu({ items, label, disabled }: Props) {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);
  const [position, setPosition] = useState<{ top: number; right: number; above: boolean } | null>(null);
  const buttonRef = useRef<HTMLButtonElement>(null);
  const menuRef = useRef<HTMLDivElement>(null);
  const menuId = useId();

  useLayoutEffect(() => {
    if (!open || !buttonRef.current) return;
    const rect = buttonRef.current.getBoundingClientRect();
    // Open upwards when there's no room below (last rows of a long table).
    const above = window.innerHeight - rect.bottom < 44 * items.length + 16;
    setPosition({
      top: above ? rect.top - 4 : rect.bottom + 4,
      right: Math.max(8, window.innerWidth - rect.right),
      above
    });
  }, [open, items.length]);

  useEffect(() => {
    if (!open) return;
    menuRef.current?.querySelector<HTMLButtonElement>("[role=menuitem]")?.focus();

    const close = () => setOpen(false);
    const onPointerDown = (e: PointerEvent) => {
      const target = e.target as Node;
      if (!menuRef.current?.contains(target) && !buttonRef.current?.contains(target)) close();
    };
    document.addEventListener("pointerdown", onPointerDown);
    window.addEventListener("scroll", close, true);
    window.addEventListener("resize", close);
    return () => {
      document.removeEventListener("pointerdown", onPointerDown);
      window.removeEventListener("scroll", close, true);
      window.removeEventListener("resize", close);
    };
  }, [open]);

  function closeAndFocusButton() {
    setOpen(false);
    buttonRef.current?.focus();
  }

  function onMenuKeyDown(e: KeyboardEvent<HTMLDivElement>) {
    const options = Array.from(menuRef.current?.querySelectorAll<HTMLButtonElement>("[role=menuitem]") ?? []);
    const index = options.indexOf(document.activeElement as HTMLButtonElement);
    if (e.key === "Escape") {
      e.preventDefault();
      closeAndFocusButton();
    } else if (e.key === "ArrowDown" || e.key === "ArrowUp") {
      e.preventDefault();
      const step = e.key === "ArrowDown" ? 1 : -1;
      options[(index + step + options.length) % options.length]?.focus();
    } else if (e.key === "Home" || e.key === "End") {
      e.preventDefault();
      options[e.key === "Home" ? 0 : options.length - 1]?.focus();
    } else if (e.key === "Tab") {
      setOpen(false);
    }
  }

  if (items.length === 0) return null;

  return (
    <>
      <button
        ref={buttonRef}
        type="button"
        className={open ? "row-menu__toggle is-open" : "row-menu__toggle"}
        aria-label={label ?? t('common.actions')}
        title={t('common.actions')}
        aria-haspopup="menu"
        aria-expanded={open}
        aria-controls={open ? menuId : undefined}
        disabled={disabled}
        onClick={() => setOpen((o) => !o)}
      >
        <Icon name="dots" />
      </button>
      {open && (
        <div
          ref={menuRef}
          id={menuId}
          role="menu"
          className="row-menu"
          style={
            position
              ? {
                  top: position.top,
                  right: position.right,
                  transform: position.above ? "translateY(-100%)" : undefined
                }
              : { visibility: "hidden" }
          }
          onKeyDown={onMenuKeyDown}
        >
          {items.map((item) => (
            <button
              key={item.label}
              type="button"
              role="menuitem"
              className={item.danger ? "row-menu__item row-menu__item--danger" : "row-menu__item"}
              onClick={() => {
                closeAndFocusButton();
                item.onSelect();
              }}
            >
              {item.label}
            </button>
          ))}
        </div>
      )}
    </>
  );
}
