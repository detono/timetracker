import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { act, render, screen } from "@testing-library/react";
import { AxiosError, type InternalAxiosRequestConfig } from "axios";
import { apiClient } from "../../api/client";
import { AuthProvider, useAuth } from "../AuthContext";
import type { AuthResult } from "../../types";

vi.mock("../../api/authApi", () => ({ setLanguage: vi.fn().mockResolvedValue(undefined) }));

const signIn: AuthResult = {
  token: "token-1",
  userId: "u1",
  fullName: "Eva Employee",
  role: "Employee",
  tenantName: "Bakkerij",
  language: "en",
  mustChangePassword: false
};

const adminSignIn: AuthResult = {
  token: "admin-token",
  userId: "a1",
  fullName: "Ada Admin",
  role: "PlatformAdmin",
  tenantName: null,
  language: "en",
  mustChangePassword: false
};

const employerSignIn: AuthResult = {
  token: "support-token",
  userId: "e1",
  fullName: "Emma Employer",
  role: "Employer",
  tenantName: "Bakkerij",
  language: "nl",
  mustChangePassword: false
};

/** Makes every API call fail with the given status and body. */
function answerWith(status: number, data: unknown = {}) {
  apiClient.defaults.adapter = async (config: InternalAxiosRequestConfig) => {
    throw new AxiosError("failed", String(status), config, null, {
      status, statusText: "", data, headers: {}, config
    });
  };
}

function Probe() {
  const auth = useAuth();
  return (
    <div>
      <span data-testid="user">{auth.user?.fullName ?? "nobody"}</span>
      <span data-testid="ended">{String(auth.sessionEnded)}</span>
      <span data-testid="must-change">{String(auth.user?.mustChangePassword ?? false)}</span>
      <span data-testid="support-by">{auth.user?.supportBy ?? "-"}</span>
      <button onClick={() => auth.login(signIn)}>sign in</button>
      <button onClick={() => auth.login(adminSignIn)}>sign in as admin</button>
      <button onClick={() => auth.startSupportSignIn(employerSignIn)}>support</button>
      <button onClick={() => auth.endSupportSignIn()}>end support</button>
      <button onClick={() => auth.logout()}>sign out</button>
    </div>
  );
}

const originalAdapter = apiClient.defaults.adapter;

beforeEach(() => localStorage.clear());
afterEach(() => {
  apiClient.defaults.adapter = originalAdapter;
});

async function renderSignedIn() {
  render(
    <AuthProvider>
      <Probe />
    </AuthProvider>
  );
  await act(async () => screen.getByText("sign in").click());
  expect(screen.getByTestId("user")).toHaveTextContent("Eva Employee");
}

describe("AuthContext", () => {
  it("keeps the sign-in across reloads", async () => {
    await renderSignedIn();
    expect(JSON.parse(localStorage.getItem("timetracker.auth")!).token).toBe("token-1");
    expect(apiClient.defaults.headers.common.Authorization).toBe("Bearer token-1");
  });

  describe("a sign-in kept from an earlier visit", () => {
    /** A JWT whose only claim that matters here is exp (seconds since 1970). */
    function jwt(exp: number) {
      const part = (o: object) => btoa(JSON.stringify(o)).replace(/=+$/, "").replace(/\+/g, "-").replace(/\//g, "_");
      return `${part({ alg: "HS256" })}.${part({ sub: "u1", exp })}.signature`;
    }

    function renderWithStoredToken(token: string) {
      localStorage.setItem("timetracker.auth", JSON.stringify({ ...signIn, token }));
      render(
        <AuthProvider>
          <Probe />
        </AuthProvider>
      );
    }

    it("is used while its token is valid", () => {
      renderWithStoredToken(jwt(Date.now() / 1000 + 3600));

      expect(screen.getByTestId("user")).toHaveTextContent("Eva Employee");
    });

    // Otherwise a link in an email opens the page as if signed in, instead of asking to sign in first.
    it("is dropped once its token has expired, saying the session ended", () => {
      renderWithStoredToken(jwt(Date.now() / 1000 - 60));

      expect(screen.getByTestId("user")).toHaveTextContent("nobody");
      expect(screen.getByTestId("ended")).toHaveTextContent("true");
      expect(localStorage.getItem("timetracker.auth")).toBeNull();
      expect(apiClient.defaults.headers.common.Authorization).toBeUndefined();
    });
  });

  it("signs out and says why when the server no longer accepts the sign-in", async () => {
    await renderSignedIn();
    answerWith(401);

    await act(async () => {
      await apiClient.get("/anything").catch(() => undefined);
    });

    expect(screen.getByTestId("user")).toHaveTextContent("nobody");
    expect(screen.getByTestId("ended")).toHaveTextContent("true");
    expect(localStorage.getItem("timetracker.auth")).toBeNull();
    expect(apiClient.defaults.headers.common.Authorization).toBeUndefined();
  });

  it("asks for a new password when the server says one is required", async () => {
    await renderSignedIn();
    answerWith(403, { code: "passwordChangeRequired" });

    await act(async () => {
      await apiClient.get("/anything").catch(() => undefined);
    });

    expect(screen.getByTestId("user")).toHaveTextContent("Eva Employee");
    expect(screen.getByTestId("must-change")).toHaveTextContent("true");
  });

  it("ignores other errors, and a 401 on the sign-in request itself", async () => {
    render(
      <AuthProvider>
        <Probe />
      </AuthProvider>
    );
    answerWith(401);
    await act(async () => {
      await apiClient.post("/auth/login", {}).catch(() => undefined);
    });
    expect(screen.getByTestId("ended")).toHaveTextContent("false");

    await act(async () => screen.getByText("sign in").click());
    answerWith(500);
    await act(async () => {
      await apiClient.get("/anything").catch(() => undefined);
    });
    expect(screen.getByTestId("user")).toHaveTextContent("Eva Employee");
    expect(screen.getByTestId("must-change")).toHaveTextContent("false");
  });

  describe("support sign-in", () => {
    async function renderAsAdminInSupport() {
      render(
        <AuthProvider>
          <Probe />
        </AuthProvider>
      );
      await act(async () => screen.getByText("sign in as admin").click());
      await act(async () => screen.getByText("support").click());
    }

    it("acts as the employer, and remembers who is helping", async () => {
      await renderAsAdminInSupport();

      expect(screen.getByTestId("user")).toHaveTextContent("Emma Employer");
      expect(screen.getByTestId("support-by")).toHaveTextContent("Ada Admin");
      expect(apiClient.defaults.headers.common.Authorization).toBe("Bearer support-token");
    });

    it("goes back to the admin's own sign-in when it ends", async () => {
      await renderAsAdminInSupport();

      await act(async () => screen.getByText("end support").click());

      expect(screen.getByTestId("user")).toHaveTextContent("Ada Admin");
      expect(screen.getByTestId("support-by")).toHaveTextContent("-");
      expect(apiClient.defaults.headers.common.Authorization).toBe("Bearer admin-token");
      expect(JSON.parse(localStorage.getItem("timetracker.auth")!).token).toBe("admin-token");
    });

    it("goes back to the admin when the support sign-in expires", async () => {
      await renderAsAdminInSupport();
      answerWith(401);

      await act(async () => {
        await apiClient.get("/anything").catch(() => undefined);
      });

      expect(screen.getByTestId("user")).toHaveTextContent("Ada Admin");
      expect(screen.getByTestId("ended")).toHaveTextContent("false");
    });

    it("signs out of both on sign-out", async () => {
      await renderAsAdminInSupport();

      await act(async () => screen.getByText("sign out").click());

      expect(screen.getByTestId("user")).toHaveTextContent("nobody");
      expect(localStorage.getItem("timetracker.auth")).toBeNull();
      expect(localStorage.getItem("timetracker.auth.admin")).toBeNull();
    });
  });
});
