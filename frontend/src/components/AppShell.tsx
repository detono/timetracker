import { NavLink, Outlet } from "react-router-dom";
import { useAuth } from "../auth/AuthContext";
import { appConfig } from "../config";

export function AppShell() {
  const { user, logout } = useAuth();

  return (
    <div className="shell">
      <header className="shell__header">
        <div className="shell__brand">
          <span className="shell__brand-mark">{appConfig.title}</span>
        </div>
        <nav className="shell__nav">
          <NavLink to="/" end className={({ isActive }) => (isActive ? "shell__nav-link is-active" : "shell__nav-link")}>
            My hours
          </NavLink>
          <NavLink to="/team" className={({ isActive }) => (isActive ? "shell__nav-link is-active" : "shell__nav-link")}>
            {user?.role === "Employer" ? "All employees" : "Team overview"}
          </NavLink>
          <NavLink to="/reports" className={({ isActive }) => (isActive ? "shell__nav-link is-active" : "shell__nav-link")}>
            Reports
          </NavLink>
          {user?.role === "Employer" && (
            <NavLink to="/employees" className={({ isActive }) => (isActive ? "shell__nav-link is-active" : "shell__nav-link")}>
              Manage employees
            </NavLink>
          )}
          {user?.role === "Employer" && (
            <NavLink to="/hour-types" className={({ isActive }) => (isActive ? "shell__nav-link is-active" : "shell__nav-link")}>
              Hour types
            </NavLink>
          )}
        </nav>
        <div className="shell__account">
          <NavLink to="/account" className="shell__account-info shell__account-info--link">
            <span className="shell__account-name">{user?.fullName}</span>
            <span className="shell__account-role">{user?.role}</span>
          </NavLink>
          <button className="btn btn--ghost" onClick={logout}>
            Sign out
          </button>
        </div>
      </header>
      <main className="shell__content">
        <Outlet />
      </main>
    </div>
  );
}
