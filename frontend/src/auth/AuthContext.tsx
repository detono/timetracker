import { createContext, useContext, useMemo, useState, type ReactNode } from "react";
import { setAuthToken } from "../api/client";
import type { AuthResult, UserRole } from "../types";

interface AuthState {
  userId: string;
  fullName: string;
  role: UserRole;
  token: string;
}

interface AuthContextValue {
  user: AuthState | null;
  isAuthenticated: boolean;
  login: (result: AuthResult) => void;
  logout: () => void;
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
          token: result.token
        };
        setAuthToken(state.token); // <-- this line was missing: keep axios's header in sync
        localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
        setUser(state);
      },
      logout: () => {
        setAuthToken(null); // <-- this line was missing too
        localStorage.removeItem(STORAGE_KEY);
        setUser(null);
      }
    }),
    [user]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return ctx;
}