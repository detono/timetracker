import { describe, expect, it, vi } from "vitest";
import { act, fireEvent, render, screen } from "@testing-library/react";
import "../../i18n";
import { ConfirmProvider, useConfirm, type ConfirmOptions } from "../ConfirmDialog";

function Harness({ options, onResult, withReason }: {
  options: ConfirmOptions;
  onResult: (result: boolean | string | null) => void;
  withReason?: boolean;
}) {
  const { confirm, askReason } = useConfirm();
  return (
    <button
      type="button"
      onClick={async () =>
        onResult(withReason ? await askReason({ ...options, reasonLabel: "Reason (optional)" }) : await confirm(options))
      }
    >
      Ask
    </button>
  );
}

function setup(options: Partial<ConfirmOptions> = {}, withReason = false) {
  const onResult = vi.fn();
  render(
    <ConfirmProvider>
      <Harness
        options={{ title: "Lock October 2026?", message: "Nobody can change its hours.", confirmLabel: "Lock", ...options }}
        onResult={onResult}
        withReason={withReason}
      />
    </ConfirmProvider>
  );
  const ask = screen.getByRole("button", { name: "Ask" });
  // fireEvent.click doesn't move the focus like a real click does.
  ask.focus();
  fireEvent.click(ask);
  return { onResult, ask, dialog: screen.getByRole("dialog", { name: "Lock October 2026?" }) };
}

describe("ConfirmDialog", () => {
  it("shows the question with its explanation and resolves true on confirm", async () => {
    const { onResult, dialog, ask } = setup();
    expect(dialog).toHaveAttribute("open");
    expect(dialog).toHaveAccessibleDescription("Nobody can change its hours.");
    expect(screen.getByRole("button", { name: "Lock" })).toHaveFocus();

    await act(async () => fireEvent.click(screen.getByRole("button", { name: "Lock" })));
    expect(onResult).toHaveBeenCalledWith(true);
    expect(dialog).not.toHaveAttribute("open");
    expect(ask).toHaveFocus();
  });

  it("resolves false on Cancel, on Escape and on a click outside", async () => {
    const first = setup();
    await act(async () => fireEvent.click(screen.getByRole("button", { name: "Cancel" })));
    expect(first.onResult).toHaveBeenLastCalledWith(false);

    fireEvent.click(first.ask);
    await act(async () => fireEvent(first.dialog, new Event("cancel", { cancelable: true })));
    expect(first.onResult).toHaveBeenLastCalledWith(false);

    fireEvent.click(first.ask);
    await act(async () => {
      fireEvent.pointerDown(first.dialog);
      fireEvent.click(first.dialog);
    });
    expect(first.onResult).toHaveBeenLastCalledWith(false);
    expect(first.onResult).toHaveBeenCalledTimes(3);
  });

  it("doesn't close when a press starts inside and ends outside", () => {
    const { onResult, dialog } = setup();
    fireEvent.pointerDown(screen.getByText("Nobody can change its hours."));
    fireEvent.click(dialog);
    expect(onResult).not.toHaveBeenCalled();
    expect(dialog).toHaveAttribute("open");
  });

  it("puts the focus on Cancel for a dangerous action", () => {
    setup({ tone: "danger", confirmLabel: "Delete" });
    expect(screen.getByRole("button", { name: "Cancel" })).toHaveFocus();
    expect(screen.getByRole("button", { name: "Delete" })).toHaveClass("btn--destructive");
  });

  it("uses its own cancel label when given one", () => {
    setup({ cancelLabel: "Keep it" });
    expect(screen.getByRole("button", { name: "Keep it" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Cancel" })).not.toBeInTheDocument();
  });

  it("asks for an optional reason and returns it trimmed, or null when cancelled", async () => {
    const { onResult, ask } = setup({}, true);
    const field = screen.getByLabelText("Reason (optional)");
    expect(field).toHaveFocus();

    fireEvent.change(field, { target: { value: "  Too busy that week  " } });
    await act(async () => fireEvent.click(screen.getByRole("button", { name: "Lock" })));
    expect(onResult).toHaveBeenLastCalledWith("Too busy that week");

    fireEvent.click(ask);
    expect(screen.getByLabelText("Reason (optional)")).toHaveValue("");
    await act(async () => fireEvent.click(screen.getByRole("button", { name: "Lock" })));
    expect(onResult).toHaveBeenLastCalledWith("");

    fireEvent.click(ask);
    await act(async () => fireEvent.click(screen.getByRole("button", { name: "Cancel" })));
    expect(onResult).toHaveBeenLastCalledWith(null);
  });
});
