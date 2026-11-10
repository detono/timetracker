import { useLocation, type Location } from "react-router-dom";

/**
 * Where to go after signing in: the page the sign-in screen was shown for (kept in the
 * router state by RequireAuth, so only paths inside the app), or home.
 */
export function useAfterLoginPath(): string {
  const from = (useLocation().state as { from?: Location } | null)?.from;
  return from && from.pathname !== "/login" ? `${from.pathname}${from.search}` : "/";
}
