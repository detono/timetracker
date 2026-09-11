import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
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
    return raw ? (JSON.parse(raw) as AuthState) : null;
  });

  useEffect(() => {
    setAuthToken(user?.token ?? null);
  }, [user]);

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
        localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
        setUser(state);
      },
      logout: () => {
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
