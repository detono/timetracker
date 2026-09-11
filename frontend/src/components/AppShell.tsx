import { NavLink, Outlet } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useAuth } from "../auth/AuthContext";
import { appConfig } from "../config";

export function AppShell() {
  const { t } = useTranslation();
  const { user, logout } = useAuth();

  const translatedRole = user?.role === "Employer" ? t('roles.employer') : t('roles.employee');

  return (
    <div className="shell">
      <header className="shell__header">
        <div className="shell__brand">
          <span className="shell__brand-mark">{appConfig.title}</span>
        </div>
        <nav className="shell__nav">
          <NavLink to="/" end className={({ isActive }) => (isActive ? "shell__nav-link is-active" : "shell__nav-link")}>
            {t('nav.myHours')}
          </NavLink>
          <NavLink to="/team" className={({ isActive }) => (isActive ? "shell__nav-link is-active" : "shell__nav-link")}>
            {user?.role === "Employer" ? t('nav.allEmployees') : t('nav.teamOverview')}
          </NavLink>
          <NavLink to="/reports" className={({ isActive }) => (isActive ? "shell__nav-link is-active" : "shell__nav-link")}>
            {t('nav.reports')}
          </NavLink>
          {user?.role === "Employer" && (
            <NavLink to="/employees" className={({ isActive }) => (isActive ? "shell__nav-link is-active" : "shell__nav-link")}>
              {t('nav.manageEmployees')}
            </NavLink>
          )}
          {user?.role === "Employer" && (
            <NavLink to="/hour-types" className={({ isActive }) => (isActive ? "shell__nav-link is-active" : "shell__nav-link")}>
              {t('nav.hourTypes')}
            </NavLink>
          )}
        </nav>
        <div className="shell__account">
          <NavLink to="/account" className="shell__account-info shell__account-info--link">
            <span className="shell__account-name">{user?.fullName}</span>
            <span className="shell__account-role">{translatedRole}</span>
          </NavLink>
          <button className="btn btn--ghost" onClick={logout}>
            {t('common.signOut')}
          </button>
        </div>
      </header>
      <main className="shell__content">
        <Outlet />
      </main>
    </div>
  );
}