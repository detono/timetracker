import { useCallback, useEffect, useState } from "react";
import { NavLink, Outlet, useLocation } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useAuth } from "../auth/AuthContext";
import { getPendingLeaveCount } from "../api/leaveApi";
import { appConfig } from "../config";
import { roleLabel } from "../utils/roles";
import type { ShellContext } from "../utils/leave";

const navLinkClass = ({ isActive }: { isActive: boolean }) => (isActive ? "shell__nav-link is-active" : "shell__nav-link");

export function AppShell() {
  const { t } = useTranslation();
  const { user, logout } = useAuth();

  const translatedRole = user ? roleLabel(t, user.role) : "";
  const isEmployer = user?.role === "Employer";
  const isTenantUser = !!user && user.role !== "PlatformAdmin";
  const location = useLocation();

  // Leave requests waiting for this user's review, refreshed on every page change.
  const [pendingLeaveCount, setPendingLeaveCount] = useState(0);
  const refreshPendingLeaveCount = useCallback(() => {
    if (!isTenantUser) return;
    getPendingLeaveCount()
      .then(setPendingLeaveCount)
      .catch(() => setPendingLeaveCount(0));
  }, [isTenantUser]);

  useEffect(() => {
    refreshPendingLeaveCount();
  }, [refreshPendingLeaveCount, location.pathname]);

  const shellContext: ShellContext = { pendingLeaveCount, refreshPendingLeaveCount };

  // On narrower screens the menu folds away behind a button; it closes again after navigating.
  const [menuOpen, setMenuOpen] = useState(false);
  useEffect(() => {
    setMenuOpen(false);
  }, [location.pathname]);

  return (
    <div className="shell">
      <header className="shell__header">
        <div className="shell__brand">
          <span className="shell__brand-mark">{appConfig.title}</span>
        </div>
        <button
          type="button"
          className="shell__menu-toggle"
          aria-controls="main-menu"
          aria-expanded={menuOpen}
          aria-label={t('nav.menu')}
          onClick={() => setMenuOpen((open) => !open)}
        >
          <svg width="22" height="22" viewBox="0 0 24 24" aria-hidden="true">
            {menuOpen ? (
              <path d="M6 6l12 12M18 6L6 18" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
            ) : (
              <path d="M4 7h16M4 12h16M4 17h16" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
            )}
          </svg>
          {pendingLeaveCount > 0 && !menuOpen && <span className="shell__menu-dot" aria-hidden="true" />}
        </button>
        <div id="main-menu" className={menuOpen ? "shell__menu is-open" : "shell__menu"}>
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
              <NavLink to="/leave" className={navLinkClass}>
                {t('nav.leave')}
                {pendingLeaveCount > 0 && (
                  <span className="nav-badge" aria-label={t('leave.pendingBadge', { count: pendingLeaveCount })}>
                    {pendingLeaveCount}
                  </span>
                )}
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
                  <NavLink to="/company" className={navLinkClass}>
                    {t('nav.company')}
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
        </div>
      </header>
      <main className="shell__content">
        <Outlet context={shellContext} />
      </main>
    </div>
  );
}
