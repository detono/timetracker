import { useCallback, useEffect, useState } from "react";
import { NavLink, Outlet, useLocation } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { useAuth } from "../auth/AuthContext";
import { getPendingLeaveCount } from "../api/leaveApi";
import { appConfig } from "../config";
import { roleLabel } from "../utils/roles";
import type { ShellContext } from "../utils/leave";
import { Icon, type IconName } from "./Icon";
import { initials } from "../utils/initials";

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

  // On narrower screens the sidebar slides in from a menu button; it closes after navigating
  // or on Escape.
  const [menuOpen, setMenuOpen] = useState(false);
  useEffect(() => {
    setMenuOpen(false);
  }, [location.pathname]);
  useEffect(() => {
    if (!menuOpen) return;
    const onKey = (e: KeyboardEvent) => e.key === "Escape" && setMenuOpen(false);
    document.addEventListener("keydown", onKey);
    return () => document.removeEventListener("keydown", onKey);
  }, [menuOpen]);

  const navItem = (to: string, icon: IconName, label: string, extra?: JSX.Element) => (
    <NavLink to={to} end={to === "/"} className={navLinkClass}>
      <Icon name={icon} />
      <span className="shell__nav-label">{label}</span>
      {extra}
    </NavLink>
  );

  return (
    <div className="shell">
      <header className="shell__topbar">
        <Brand />
        <button
          type="button"
          className="shell__menu-toggle"
          aria-controls="main-menu"
          aria-expanded={menuOpen}
          aria-label={t('nav.menu')}
          onClick={() => setMenuOpen((open) => !open)}
        >
          <Icon name={menuOpen ? "close" : "menu"} size={22} />
          {pendingLeaveCount > 0 && !menuOpen && <span className="shell__menu-dot" aria-hidden="true" />}
        </button>
      </header>

      <aside id="main-menu" className={menuOpen ? "shell__sidebar is-open" : "shell__sidebar"}>
        <div className="shell__sidebar-brand">
          <Brand />
        </div>
        <nav className="shell__nav">
          {user?.role === "PlatformAdmin" ? (
            <>
              {navItem("/tenants", "building", t('nav.tenants'))}
              {navItem("/platform-admins", "shield", t('nav.platformAdmins'))}
            </>
          ) : (
            <>
              {navItem("/", "clock", t('nav.myHours'))}
              {navItem("/team", "team", isEmployer ? t('nav.allEmployees') : t('nav.teamOverview'))}
              {navItem(
                "/leave",
                "leave",
                t('nav.leave'),
                pendingLeaveCount > 0 ? (
                  <span className="nav-badge" aria-label={t('leave.pendingBadge', { count: pendingLeaveCount })}>
                    {pendingLeaveCount}
                  </span>
                ) : undefined
              )}
              {navItem("/reports", "reports", t('nav.reports'))}
              {isEmployer && (
                <>
                  <span className="shell__nav-section">{t('nav.manage')}</span>
                  {navItem("/employees", "people", t('nav.manageEmployees'))}
                  {navItem("/hour-types", "tag", t('nav.hourTypes'))}
                  {navItem("/projects", "folder", t('nav.projects'))}
                  {navItem("/company", "building", t('nav.company'))}
                </>
              )}
            </>
          )}
        </nav>
        <div className="shell__account">
          <NavLink to="/account" className="shell__account-info">
            <span className="shell__avatar" aria-hidden="true">{initials(user?.fullName)}</span>
            <span className="shell__account-text">
              <span className="shell__account-name">{user?.fullName}</span>
              <span className="shell__account-role">
                {user?.tenantName ? `${translatedRole} · ${user.tenantName}` : translatedRole}
              </span>
            </span>
          </NavLink>
          <button
            type="button"
            className="shell__signout"
            onClick={logout}
            aria-label={t('common.signOut')}
            title={t('common.signOut')}
          >
            <Icon name="logout" />
          </button>
        </div>
      </aside>
      {menuOpen && <div className="shell__backdrop" aria-hidden="true" onClick={() => setMenuOpen(false)} />}

      <main className="shell__content">
        <Outlet context={shellContext} />
      </main>
    </div>
  );
}

function Brand() {
  return (
    <span className="shell__brand">
      <span className="shell__brand-icon">
        <Icon name="clock" size={18} />
      </span>
      <span className="shell__brand-name">{appConfig.title}</span>
    </span>
  );
}
