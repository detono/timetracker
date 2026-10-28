import { createContext, useContext, useMemo, useState, type ReactNode } from "react";
import { setAuthToken } from "../api/client";
import { setLanguage } from "../api/authApi";
import i18n from "../i18n";
import type { AuthResult, UserRole } from "../types";

interface AuthState {
  userId: string;
  fullName: string;
  role: UserRole;
  tenantName: string | null;
  token: string;
}

interface AuthContextValue {
  user: AuthState | null;
  isAuthenticated: boolean;
  login: (result: AuthResult) => void;
  logout: () => void;
  /** Shows a renamed company in the header without signing in again. */
  updateTenantName: (name: string) => void;
}

const STORAGE_KEY = "timetracker.auth";

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthState | null>(() => {
    const raw = localStorage.getItem(STORAGE_KEY);
    const initial = raw ? (JSON.parse(raw) as AuthState) : null;
    setAuthToken(initial?.token ?? null);
    return initial;
  });

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: !!user,
      login: (result: AuthResult) => {
        const state: AuthState = {
          userId: result.userId,
          fullName: result.fullName,
          role: result.role,
          tenantName: result.tenantName,
          token: result.token
        };
        setAuthToken(state.token);
        localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
        setUser(state);

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
        localStorage.removeItem(STORAGE_KEY);
        setUser(null);
      },
      updateTenantName: (name: string) => {
        if (!user) return;
        const state = { ...user, tenantName: name };
        localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
        setUser(state);
      }
    }),
    [user]
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
