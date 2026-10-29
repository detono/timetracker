import { describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen } from "@testing-library/react";
import "../../i18n";
import { RowMenu } from "../RowMenu";
import { initials } from "../../utils/initials";

function setup() {
  const onEdit = vi.fn();
  const onDelete = vi.fn();
  render(
    <RowMenu
      label="Actions: Jane"
      items={[
        { label: "Edit", onSelect: onEdit },
        { label: "Delete", onSelect: onDelete, danger: true }
      ]}
    />
  );
  return { onEdit, onDelete, toggle: screen.getByRole("button", { name: "Actions: Jane" }) };
}

describe("RowMenu", () => {
  it("opens on click, focuses the first item and runs the chosen action", () => {
    const { onEdit, onDelete, toggle } = setup();
    expect(screen.queryByRole("menu")).not.toBeInTheDocument();

    fireEvent.click(toggle);
    expect(toggle).toHaveAttribute("aria-expanded", "true");
    expect(screen.getByRole("menuitem", { name: "Edit" })).toHaveFocus();

    fireEvent.click(screen.getByRole("menuitem", { name: "Delete" }));
    expect(onDelete).toHaveBeenCalledOnce();
    expect(onEdit).not.toHaveBeenCalled();
    expect(screen.queryByRole("menu")).not.toBeInTheDocument();
    expect(toggle).toHaveFocus();
  });

  it("moves with the arrow keys and closes on Escape", () => {
    const { toggle } = setup();
    fireEvent.click(toggle);
    const menu = screen.getByRole("menu");

    fireEvent.keyDown(menu, { key: "ArrowDown" });
    expect(screen.getByRole("menuitem", { name: "Delete" })).toHaveFocus();
    fireEvent.keyDown(menu, { key: "ArrowDown" });
    expect(screen.getByRole("menuitem", { name: "Edit" })).toHaveFocus();

    fireEvent.keyDown(menu, { key: "Escape" });
    expect(screen.queryByRole("menu")).not.toBeInTheDocument();
    expect(toggle).toHaveFocus();
  });

  it("closes when clicking elsewhere", () => {
    const { toggle } = setup();
    fireEvent.click(toggle);
    fireEvent.pointerDown(document.body);
    expect(screen.queryByRole("menu")).not.toBeInTheDocument();
  });

  it("marks dangerous actions and renders nothing without items", () => {
    const { toggle } = setup();
    fireEvent.click(toggle);
    expect(screen.getByRole("menuitem", { name: "Delete" })).toHaveClass("row-menu__item--danger");

    const { container } = render(<RowMenu items={[]} />);
    expect(container).toBeEmptyDOMElement();
  });
});

describe("initials", () => {
  it("takes the first letters of the first and last word", () => {
    expect(initials("Jane van Dam")).toBe("JD");
    expect(initials("  eva  ")).toBe("E");
    expect(initials("")).toBe("?");
    expect(initials(undefined)).toBe("?");
  });
});
