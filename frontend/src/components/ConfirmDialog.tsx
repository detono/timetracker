import { createContext, useCallback, useContext, useEffect, useId, useMemo, useRef, useState, type FormEvent, type ReactNode } from "react";
import { useTranslation } from "react-i18next";

export interface ConfirmOptions {
  title: string;
  /** Extra explanation under the title: what happens, and whether it can be undone. */
  message?: string;
  confirmLabel: string;
  /** Defaults to "Cancel". Set it when the action itself is called "Cancel" in some language. */
  cancelLabel?: string;
  /** "danger" shows the confirm button in red and puts the focus on Cancel. */
  tone?: "default" | "danger";
}

export interface ReasonOptions extends ConfirmOptions {
  /** Label of the optional text field. */
  reasonLabel: string;
}

interface ConfirmApi {
  /** Resolves to true when confirmed, false when cancelled. */
  confirm: (options: ConfirmOptions) => Promise<boolean>;
  /** Resolves to the trimmed reason ("" when left empty), or null when cancelled. */
  askReason: (options: ReasonOptions) => Promise<string | null>;
}

interface Request {
  options: ConfirmOptions;
  reasonLabel?: string;
  resolve: (result: { confirmed: boolean; reason: string }) => void;
}

const ConfirmContext = createContext<ConfirmApi | null>(null);

/**
 * Asks "are you sure?" in the app's own style instead of the browser's confirm() and prompt().
 * Mount it once around the app; pages call useConfirm(). Escape, Cancel and a click outside
 * the dialog all cancel.
 */
export function ConfirmProvider({ children }: { children: ReactNode }) {
  const { t } = useTranslation();
  const [request, setRequest] = useState<Request | null>(null);
  const [reason, setReason] = useState("");
  const dialogRef = useRef<HTMLDialogElement>(null);
  const confirmRef = useRef<HTMLButtonElement>(null);
  const cancelRef = useRef<HTMLButtonElement>(null);
  const reasonRef = useRef<HTMLTextAreaElement>(null);
  const returnFocusRef = useRef<HTMLElement | null>(null);
  const pressedOnBackdrop = useRef(false);
  const titleId = useId();
  const messageId = useId();
  const reasonId = useId();

  const open = useCallback((options: ConfirmOptions, reasonLabel?: string) => {
    return new Promise<{ confirmed: boolean; reason: string }>((resolve) => {
      setRequest((previous) => {
        // Only one question at a time: a new one cancels the one still open.
        previous?.resolve({ confirmed: false, reason: "" });
        return { options, reasonLabel, resolve };
      });
      setReason("");
    });
  }, []);

  const api = useMemo<ConfirmApi>(
    () => ({
      confirm: async (options) => (await open(options)).confirmed,
      askReason: async ({ reasonLabel, ...options }) => {
        const result = await open(options, reasonLabel);
        return result.confirmed ? result.reason.trim() : null;
      }
    }),
    [open]
  );

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!request || !dialog) return;
    if (!dialog.open) {
      returnFocusRef.current = document.activeElement as HTMLElement | null;
      dialog.showModal();
    }
    const first =
      request.reasonLabel !== undefined
        ? reasonRef.current
        : request.options.tone === "danger"
          ? cancelRef.current
          : confirmRef.current;
    first?.focus();
  }, [request]);

  function finish(confirmed: boolean) {
    if (!request) return;
    request.resolve({ confirmed, reason });
    setRequest(null);
    dialogRef.current?.close();
    returnFocusRef.current?.focus();
    returnFocusRef.current = null;
  }

  function onSubmit(e: FormEvent) {
    e.preventDefault();
    finish(true);
  }

  const options = request?.options;
  const danger = options?.tone === "danger";

  return (
    <ConfirmContext.Provider value={api}>
      {children}
      <dialog
        ref={dialogRef}
        className="confirm-dialog"
        aria-labelledby={titleId}
        aria-describedby={options?.message ? messageId : undefined}
        onCancel={(e) => {
          // Escape: close through finish() so the caller gets its answer.
          e.preventDefault();
          finish(false);
        }}
        onPointerDown={(e) => {
          pressedOnBackdrop.current = e.target === e.currentTarget;
        }}
        onClick={(e) => {
          // The dialog element itself is only hit outside the content box, on the backdrop.
          // A press that started inside (e.g. selecting text) and ended outside doesn't count.
          if (e.target === e.currentTarget && pressedOnBackdrop.current) finish(false);
          pressedOnBackdrop.current = false;
        }}
      >
        {options && (
          <form className="confirm-dialog__body" onSubmit={onSubmit}>
            <h2 id={titleId} className="confirm-dialog__title">{options.title}</h2>
            {options.message && (
              <p id={messageId} className="confirm-dialog__message">{options.message}</p>
            )}
            {request.reasonLabel !== undefined && (
              <div className="form__field confirm-dialog__reason">
                <label htmlFor={reasonId}>{request.reasonLabel}</label>
                <textarea
                  id={reasonId}
                  ref={reasonRef}
                  rows={3}
                  value={reason}
                  onChange={(e) => setReason(e.target.value)}
                />
              </div>
            )}
            <div className="confirm-dialog__actions">
              <button ref={cancelRef} type="button" className="btn btn--ghost" onClick={() => finish(false)}>
                {options.cancelLabel ?? t('common.cancel')}
              </button>
              <button
                ref={confirmRef}
                type="submit"
                className={danger ? "btn btn--primary btn--destructive" : "btn btn--primary"}
              >
                {options.confirmLabel}
              </button>
            </div>
          </form>
        )}
      </dialog>
    </ConfirmContext.Provider>
  );
}

// eslint-disable-next-line react-refresh/only-export-components
export function useConfirm(): ConfirmApi {
  const api = useContext(ConfirmContext);
  if (!api) throw new Error("useConfirm must be used inside <ConfirmProvider>.");
  return api;
}
