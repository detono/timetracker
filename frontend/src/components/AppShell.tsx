import { NavLink, Outlet } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useAuth } from "../auth/AuthContext";
import { appConfig } from "../config";
import { roleLabel } from "../utils/roles";

const navLinkClass = ({ isActive }: { isActive: boolean }) => (isActive ? "shell__nav-link is-active" : "shell__nav-link");

export function AppShell() {
  const { t } = useTranslation();
  const { user, logout } = useAuth();

  const translatedRole = user ? roleLabel(t, user.role) : "";
  const isEmployer = user?.role === "Employer";

  return (
    <div className="shell">
      <header className="shell__header">
        <div className="shell__brand">
          <span className="shell__brand-mark">{appConfig.title}</span>
        </div>
        <nav className="shell__nav">
          {user?.role === "PlatformAdmin" ? (
            <>
              <NavLink to="/tenants" className={navLinkClass}>
                {t('nav.tenants')}
              </NavLink>
              <NavLink to="/platform-admins" className={navLinkClass}>
                {t('nav.platformAdmins')}
              </NavLink>
            </>
          ) : (
            <>
              <NavLink to="/" end className={navLinkClass}>
                {t('nav.myHours')}
              </NavLink>
              <NavLink to="/team" className={navLinkClass}>
                {isEmployer ? t('nav.allEmployees') : t('nav.teamOverview')}
              </NavLink>
              <NavLink to="/reports" className={navLinkClass}>
                {t('nav.reports')}
              </NavLink>
              {isEmployer && (
                <>
                  <NavLink to="/employees" className={navLinkClass}>
                    {t('nav.manageEmployees')}
                  </NavLink>
                  <NavLink to="/hour-types" className={navLinkClass}>
                    {t('nav.hourTypes')}
                  </NavLink>
                  <NavLink to="/projects" className={navLinkClass}>
                    {t('nav.projects')}
                  </NavLink>
                </>
              )}
            </>
          )}
        </nav>
        <div className="shell__account">
          <NavLink to="/account" className="shell__account-info shell__account-info--link">
            <span className="shell__account-name">{user?.fullName}</span>
            <span className="shell__account-role">
              {user?.tenantName ? `${translatedRole} · ${user.tenantName}` : translatedRole}
            </span>
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
