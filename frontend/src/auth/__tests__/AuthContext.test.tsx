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
      <button onClick={() => auth.login(signIn)}>sign in</button>
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
});
