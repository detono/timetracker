import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import "../../i18n";
import { WhatsNewDialog } from "../WhatsNewDialog";
import { InlineMarkdown } from "../InlineMarkdown";
import { getWhatsNew, markWhatsNewSeen } from "../../api/whatsNewApi";

vi.mock("../../api/whatsNewApi", () => ({
  getWhatsNew: vi.fn(),
  markWhatsNewSeen: vi.fn(async () => undefined)
}));

const releases = [
  {
    version: "1.4.0",
    date: "2026-11-10",
    sections: [
      { title: "Added", items: ["**Webhooks** on the *Company* page."] },
      { title: "Fixed", items: ["Choosing a supervisor failed."] }
    ]
  },
  { version: "1.3.2", date: "2026-11-08", sections: [{ title: "Changed", items: ["Projects have budgets."] }] }
];

describe("WhatsNewDialog", () => {
  beforeEach(() => {
    vi.mocked(markWhatsNewSeen).mockClear();
  });

  it("shows the unseen releases and remembers the newest once closed", async () => {
    vi.mocked(getWhatsNew).mockResolvedValue({ releases });
    render(<WhatsNewDialog />);

    expect(await screen.findByRole("heading", { name: "What's new" })).toBeInTheDocument();
    expect(screen.getByText("Version 1.4.0")).toBeInTheDocument();
    expect(screen.getByText("Version 1.3.2")).toBeInTheDocument();
    expect(screen.getByText("New")).toBeInTheDocument();
    expect(screen.getByText("Webhooks").tagName).toBe("STRONG");

    fireEvent.click(screen.getByRole("button", { name: "Got it" }));

    expect(markWhatsNewSeen).toHaveBeenCalledWith("1.4.0");
    expect(screen.queryByText("Version 1.4.0")).not.toBeInTheDocument();
  });

  it("stays hidden when there's nothing new", async () => {
    vi.mocked(getWhatsNew).mockResolvedValue({ releases: [] });
    const { container } = render(<WhatsNewDialog />);

    await waitFor(() => expect(getWhatsNew).toHaveBeenCalled());
    expect(container.querySelector("dialog")).not.toHaveAttribute("open");
    expect(markWhatsNewSeen).not.toHaveBeenCalled();
  });

  it("stays hidden when the notes can't load", async () => {
    vi.mocked(getWhatsNew).mockRejectedValue(new Error("offline"));
    const { container } = render(<WhatsNewDialog />);

    await waitFor(() => expect(getWhatsNew).toHaveBeenCalled());
    expect(container.querySelector("dialog")).not.toHaveAttribute("open");
  });
});

describe("InlineMarkdown", () => {
  it("renders code, bold, italic and web links", () => {
    const { container } = render(
      <InlineMarkdown text="Set `APP_URL` in **Company** → *Email*, see [the README](https://example.com/readme)." />
    );
    expect(container.querySelector("code")).toHaveTextContent("APP_URL");
    expect(container.querySelector("strong")).toHaveTextContent("Company");
    expect(container.querySelector("em")).toHaveTextContent("Email");
    const link = screen.getByRole("link", { name: "the README" });
    expect(link).toHaveAttribute("href", "https://example.com/readme");
    expect(link).toHaveAttribute("rel", "noopener noreferrer");
  });

  it("keeps HTML and non-web links as plain text", () => {
    const { container } = render(<InlineMarkdown text={'<img src=x onerror="alert(1)"> [click](javascript:alert(1))'} />);
    expect(container.querySelector("img")).toBeNull();
    expect(container.querySelector("a")).toBeNull();
    expect(container).toHaveTextContent('<img src=x onerror="alert(1)"> [click](javascript:alert(1))');
  });
});
