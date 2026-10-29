import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import axios from "axios";
import { apiClient, setAuthToken } from "../api/client";
import { setLanguage } from "../api/authApi";
import i18n from "../i18n";
import type { AuthResult, UserRole } from "../types";

interface AuthState {
  userId: string;
  fullName: string;
  role: UserRole;
  tenantName: string | null;
  token: string;
  /** Someone else set the password: nothing else works until the user picks their own. */
  mustChangePassword: boolean;
}

interface AuthContextValue {
  user: AuthState | null;
  isAuthenticated: boolean;
  /** True after the server ended the sign-in (deactivated, password reset, expired). */
  sessionEnded: boolean;
  login: (result: AuthResult) => void;
  logout: () => void;
  /** Shows a renamed company in the header without signing in again. */
  updateTenantName: (name: string) => void;
}

const STORAGE_KEY = "timetracker.auth";

/** Returned by the API while a temporary password still has to be replaced. */
const PASSWORD_CHANGE_REQUIRED = "passwordChangeRequired";

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function readStoredUser(): AuthState | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as AuthState) : null;
  } catch {
    return null;
  }
}

function store(state: AuthState | null) {
  try {
    if (state) localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
    else localStorage.removeItem(STORAGE_KEY);
  } catch {
    // Storage can be unavailable (private mode); the session then lasts until reload.
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthState | null>(() => {
    const initial = readStoredUser();
    setAuthToken(initial?.token ?? null);
    return initial;
  });
  const [sessionEnded, setSessionEnded] = useState(false);

  // A 401 on a signed-in request means the server no longer accepts this sign-in: the
  // account or company was deactivated, the password was reset, or the token expired.
  useEffect(() => {
    const id = apiClient.interceptors.response.use(undefined, (error) => {
      if (axios.isAxiosError(error) && error.config?.headers?.Authorization) {
        const status = error.response?.status;
        const code = (error.response?.data as { code?: string } | undefined)?.code;
        if (status === 401) {
          setAuthToken(null);
          store(null);
          setUser(null);
          setSessionEnded(true);
        } else if (status === 403 && code === PASSWORD_CHANGE_REQUIRED) {
          setUser((current) => {
            if (!current) return current;
            const next = { ...current, mustChangePassword: true };
            store(next);
            return next;
          });
        }
      }
      return Promise.reject(error);
    });
    return () => apiClient.interceptors.response.eject(id);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: !!user,
      sessionEnded,
      login: (result: AuthResult) => {
        const state: AuthState = {
          userId: result.userId,
          fullName: result.fullName,
          role: result.role,
          tenantName: result.tenantName,
          token: result.token,
          mustChangePassword: result.mustChangePassword ?? false
        };
        setAuthToken(state.token);
        store(state);
        setUser(state);
        setSessionEnded(false);

        // Use the language saved on the account; the first time, save the one the browser picked
        // so emails arrive in it too.
        if (result.language) {
          i18n.changeLanguage(result.language);
        } else if (i18n.resolvedLanguage) {
          setLanguage(i18n.resolvedLanguage).catch(() => undefined);
        }
      },
      logout: () => {
        setAuthToken(null);
        store(null);
        setUser(null);
        setSessionEnded(false);
      },
      updateTenantName: (name: string) => {
        if (!user) return;
        const state = { ...user, tenantName: name };
        store(state);
        setUser(state);
      }
    }),
    [user, sessionEnded]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return ctx;
}
