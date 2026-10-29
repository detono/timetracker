import { beforeEach, describe, expect, it, vi } from "vitest";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import "../../i18n";
import { AuthProvider } from "../../auth/AuthContext";
import * as authApi from "../../api/authApi";
import { LoginPage } from "../LoginPage";
import { ForgotPasswordPage } from "../ForgotPasswordPage";
import { ChoosePasswordPage } from "../ChoosePasswordPage";
import type { AuthResult } from "../../types";

vi.mock("../../api/authApi", () => ({
  login: vi.fn(),
  setLanguage: vi.fn().mockResolvedValue(undefined),
  changePassword: vi.fn(),
  getPasswordResetAvailable: vi.fn(),
  requestPasswordReset: vi.fn().mockResolvedValue(undefined),
  confirmPasswordReset: vi.fn().mockResolvedValue(undefined)
}));

const api = vi.mocked(authApi);

const signedIn: AuthResult = {
  token: "t", userId: "u1", fullName: "Eva Employee", role: "Employee",
  tenantName: "Bakkerij", language: "en", mustChangePassword: false
};

function renderPage(page: JSX.Element) {
  return render(
    <MemoryRouter>
      <AuthProvider>{page}</AuthProvider>
    </MemoryRouter>
  );
}

const field = (label: string) => screen.getByLabelText(label);

beforeEach(() => {
  vi.clearAllMocks();
  localStorage.clear();
});

describe("LoginPage", () => {
  it("offers 'forgot password' only when some company can send the email", async () => {
    api.getPasswordResetAvailable.mockResolvedValue(true);
    renderPage(<LoginPage />);
    expect(await screen.findByRole("link", { name: "Forgot your password?" })).toHaveAttribute("href", "/forgot-password");
  });

  it("otherwise tells people to ask their employer", async () => {
    api.getPasswordResetAvailable.mockResolvedValue(false);
    renderPage(<LoginPage />);
    await waitFor(() => expect(api.getPasswordResetAvailable).toHaveBeenCalled());
    expect(screen.getByText("Forgot your password? Ask your employer to reset it.")).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Forgot your password?" })).toBeNull();
  });
});

describe("ForgotPasswordPage", () => {
  it("emails a code, then sets the new password and signs in", async () => {
    api.login.mockResolvedValue(signedIn);
    renderPage(<ForgotPasswordPage />);

    fireEvent.change(field("Email"), { target: { value: " eva@bakkerij.test " } });
    fireEvent.click(screen.getByRole("button", { name: "Email me a code" }));
    expect(await screen.findByText(/a code is on its way/)).toBeInTheDocument();
    expect(api.requestPasswordReset).toHaveBeenCalledWith("eva@bakkerij.test");

    fireEvent.change(field("Code from the email"), { target: { value: "k7qm 3xtp" } });
    fireEvent.change(field("New password"), { target: { value: "Remembered123!" } });
    fireEvent.change(field("Confirm new password"), { target: { value: "Remembered123!" } });
    fireEvent.click(screen.getByRole("button", { name: "Set new password" }));

    await waitFor(() => expect(api.login).toHaveBeenCalledWith("eva@bakkerij.test", "Remembered123!"));
    expect(api.confirmPasswordReset).toHaveBeenCalledWith("eva@bakkerij.test", "k7qm 3xtp", "Remembered123!");
    expect(JSON.parse(localStorage.getItem("timetracker.auth")!).fullName).toBe("Eva Employee");
  });

  it("doesn't send mismatching passwords", async () => {
    renderPage(<ForgotPasswordPage />);
    fireEvent.change(field("Email"), { target: { value: "eva@bakkerij.test" } });
    fireEvent.click(screen.getByRole("button", { name: "Email me a code" }));
    await screen.findByText(/a code is on its way/);

    fireEvent.change(field("Code from the email"), { target: { value: "K7QM-3XTP" } });
    fireEvent.change(field("New password"), { target: { value: "Remembered123!" } });
    fireEvent.change(field("Confirm new password"), { target: { value: "Different123!" } });
    fireEvent.click(screen.getByRole("button", { name: "Set new password" }));

    expect(await screen.findByText("New password and confirmation don't match.")).toBeInTheDocument();
    expect(api.confirmPasswordReset).not.toHaveBeenCalled();
  });
});

describe("ChoosePasswordPage", () => {
  it("replaces a temporary password without asking for it again", async () => {
    api.changePassword.mockResolvedValue({ ...signedIn, token: "fresh" });
    renderPage(<ChoosePasswordPage />);

    fireEvent.change(field("New password"), { target: { value: "MyOwnPassword1!" } });
    fireEvent.change(field("Confirm new password"), { target: { value: "MyOwnPassword1!" } });
    fireEvent.click(screen.getByRole("button", { name: "Save and continue" }));

    await waitFor(() => expect(api.changePassword).toHaveBeenCalledWith(null, "MyOwnPassword1!"));
    expect(JSON.parse(localStorage.getItem("timetracker.auth")!).token).toBe("fresh");
  });
});
