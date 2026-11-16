import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
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
  /** Set during a support sign-in: the platform admin who is signed in as this user. */
  supportBy?: string;
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
  /** Platform admin: continue as an employer (support). Their own sign-in is kept aside. */
  startSupportSignIn: (result: AuthResult) => void;
  /** Back to the platform admin's own sign-in. */
  endSupportSignIn: () => void;
}

const STORAGE_KEY = "timetracker.auth";
/** The platform admin's own sign-in, kept aside during a support sign-in. */
const ADMIN_STORAGE_KEY = "timetracker.auth.admin";

/** Returned by the API while a temporary password still has to be replaced. */
const PASSWORD_CHANGE_REQUIRED = "passwordChangeRequired";

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function readStoredUser(key = STORAGE_KEY): AuthState | null {
  try {
    const raw = localStorage.getItem(key);
    return raw ? (JSON.parse(raw) as AuthState) : null;
  } catch {
    return null;
  }
}

function store(state: AuthState | null, key = STORAGE_KEY) {
  try {
    if (state) localStorage.setItem(key, JSON.stringify(state));
    else localStorage.removeItem(key);
  } catch {
    // Storage can be unavailable (private mode); the session then lasts until reload.
  }
}

function toState(result: AuthResult): AuthState {
  return {
    userId: result.userId,
    fullName: result.fullName,
    role: result.role,
    tenantName: result.tenantName,
    token: result.token,
    mustChangePassword: result.mustChangePassword ?? false
  };
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthState | null>(() => {
    const initial = readStoredUser();
    setAuthToken(initial?.token ?? null);
    return initial;
  });
  const [sessionEnded, setSessionEnded] = useState(false);

  /** Ends a support sign-in by putting the admin's own back. False when there was none. */
  const restoreAdmin = useCallback((): boolean => {
    const admin = readStoredUser(ADMIN_STORAGE_KEY);
    store(null, ADMIN_STORAGE_KEY);
    if (!admin) return false;
    setAuthToken(admin.token);
    store(admin);
    setUser(admin);
    return true;
  }, []);

  // A 401 on a signed-in request means the server no longer accepts this sign-in: the
  // account or company was deactivated, the password was reset, or the token expired.
  useEffect(() => {
    const id = apiClient.interceptors.response.use(undefined, (error) => {
      if (axios.isAxiosError(error) && error.config?.headers?.Authorization) {
        const status = error.response?.status;
        const code = (error.response?.data as { code?: string } | undefined)?.code;
        // A support sign-in that expired (or was ended) goes back to the admin's own.
        if (status === 401 && restoreAdmin()) {
          // Nothing else to do.
        } else if (status === 401) {
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
  }, [restoreAdmin]);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: !!user,
      sessionEnded,
      login: (result: AuthResult) => {
        const state = toState(result);
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
        store(null, ADMIN_STORAGE_KEY);
        setUser(null);
        setSessionEnded(false);
      },
      updateTenantName: (name: string) => {
        if (!user) return;
        const state = { ...user, tenantName: name };
        store(state);
        setUser(state);
      },
      startSupportSignIn: (result: AuthResult) => {
        if (!user || user.role !== "PlatformAdmin") return;
        // The admin keeps their own language; the employer's isn't touched either.
        const state: AuthState = { ...toState(result), supportBy: user.fullName };
        store(user, ADMIN_STORAGE_KEY);
        setAuthToken(state.token);
        store(state);
        setUser(state);
      },
      endSupportSignIn: () => {
        restoreAdmin();
      }
    }),
    [user, sessionEnded, restoreAdmin]
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
