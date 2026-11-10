import { useEffect, useId, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import { getWhatsNew, markWhatsNewSeen } from "../api/whatsNewApi";
import { dateLocale } from "../utils/dateRange";
import type { Release } from "../types";
import { InlineMarkdown } from "./InlineMarkdown";

// Keep a Changelog's section names; anything else is shown as written.
const knownSections = ["added", "changed", "deprecated", "removed", "fixed", "security"];

/**
 * Shows once, after an update, what changed since you last looked (from CHANGELOG.md).
 * Closing it, however you do, remembers on your account that you've seen it.
 */
export function WhatsNewDialog() {
  const { t } = useTranslation();
  const [releases, setReleases] = useState<Release[]>([]);
  const dialogRef = useRef<HTMLDialogElement>(null);
  const closeRef = useRef<HTMLButtonElement>(null);
  const pressedOnBackdrop = useRef(false);
  const titleId = useId();

  useEffect(() => {
    let cancelled = false;
    getWhatsNew()
      .then((data) => {
        if (!cancelled) setReleases(data.releases);
      })
      // The notes are a nicety: if they can't load, the app works as usual.
      .catch(() => undefined);
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (releases.length === 0 || !dialog || dialog.open) return;
    dialog.showModal();
    closeRef.current?.focus();
  }, [releases]);

  function close() {
    const newest = releases[0];
    dialogRef.current?.close();
    setReleases([]);
    // If saving fails, the notes show again next time, which is harmless.
    if (newest) markWhatsNewSeen(newest.version).catch(() => undefined);
  }

  const sectionTitle = (title: string) => {
    const key = title.toLowerCase();
    return knownSections.includes(key) ? t(`whatsNew.sections.${key}`) : title;
  };

  const formatDate = (iso: string) =>
    new Date(iso + "T00:00:00").toLocaleDateString(dateLocale(), { day: "numeric", month: "long", year: "numeric" });

  return (
    <dialog
      ref={dialogRef}
      className="confirm-dialog whats-new"
      aria-labelledby={titleId}
      onCancel={(e) => {
        e.preventDefault();
        close();
      }}
      onPointerDown={(e) => {
        pressedOnBackdrop.current = e.target === e.currentTarget;
      }}
      onClick={(e) => {
        if (e.target === e.currentTarget && pressedOnBackdrop.current) close();
        pressedOnBackdrop.current = false;
      }}
    >
      {releases.length > 0 && (
        <div className="confirm-dialog__body">
          <h2 id={titleId} className="confirm-dialog__title">{t('whatsNew.title')}</h2>
          <p className="confirm-dialog__message">{t('whatsNew.intro')}</p>
          <div className="whats-new__releases">
            {releases.map((release) => (
              <section key={release.version} className="whats-new__release">
                <h3 className="whats-new__version">
                  {t('whatsNew.version', { version: release.version })}
                  <span className="whats-new__date">{formatDate(release.date)}</span>
                </h3>
                {release.sections.map((section) => (
                  <div key={section.title}>
                    <h4 className="whats-new__section">{sectionTitle(section.title)}</h4>
                    <ul className="whats-new__items">
                      {section.items.map((item, i) => (
                        <li key={i}>
                          <InlineMarkdown text={item} />
                        </li>
                      ))}
                    </ul>
                  </div>
                ))}
              </section>
            ))}
          </div>
          <div className="confirm-dialog__actions">
            <button ref={closeRef} type="button" className="btn btn--primary" onClick={close}>
              {t('whatsNew.close')}
            </button>
          </div>
        </div>
      )}
    </dialog>
  );
}
