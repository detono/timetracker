import { Navigate, Route, Routes } from "react-router-dom";
import { useAuth } from "./auth/AuthContext";
import { AppShell } from "./components/AppShell";
import { LoginPage } from "./pages/LoginPage";
import { DashboardPage } from "./pages/DashboardPage";
import { ReportsPage } from "./pages/ReportsPage";
import { TeamPage } from "./pages/TeamPage";
import { EmployeesPage } from "./pages/EmployeesPage";
import { AccountPage } from "./pages/AccountPage";
import { HourTypesPage } from "./pages/HourTypesPage";
import { ProjectsPage } from "./pages/ProjectsPage";
import { TenantsPage } from "./pages/TenantsPage";
import { PlatformAdminsPage } from "./pages/PlatformAdminsPage";
import { EmailSettingsPage } from "./pages/EmailSettingsPage";
import { LeavePage } from "./pages/LeavePage";

function RequireAuth({ children }: { children: JSX.Element }) {
  const { isAuthenticated } = useAuth();
  return isAuthenticated ? children : <Navigate to="/login" replace />;
}

/** Company pages. A platform admin has no company, so they're sent to their own home page. */
function RequireTenantUser({ children }: { children: JSX.Element }) {
  const { user } = useAuth();
  return user?.role === "PlatformAdmin" ? <Navigate to="/tenants" replace /> : children;
}

function RequireEmployer({ children }: { children: JSX.Element }) {
  const { user } = useAuth();
  return user?.role === "Employer" ? children : <Navigate to="/" replace />;
}

function RequirePlatformAdmin({ children }: { children: JSX.Element }) {
  const { user } = useAuth();
  return user?.role === "PlatformAdmin" ? children : <Navigate to="/" replace />;
}

export default function App() {
  const { isAuthenticated } = useAuth();

  return (
    <Routes>
      <Route path="/login" element={isAuthenticated ? <Navigate to="/" replace /> : <LoginPage />} />
      <Route
        path="/"
        element={
          <RequireAuth>
            <AppShell />
          </RequireAuth>
        }
      >
        <Route
          index
          element={
            <RequireTenantUser>
              <DashboardPage />
            </RequireTenantUser>
          }
        />
        <Route
          path="reports"
          element={
            <RequireTenantUser>
              <ReportsPage />
            </RequireTenantUser>
          }
        />
        <Route
          path="team"
          element={
            <RequireTenantUser>
              <TeamPage />
            </RequireTenantUser>
          }
        />
        <Route
          path="leave"
          element={
            <RequireTenantUser>
              <LeavePage />
            </RequireTenantUser>
          }
        />
        <Route path="account" element={<AccountPage />} />
        <Route
          path="employees"
          element={
            <RequireEmployer>
              <EmployeesPage />
            </RequireEmployer>
          }
        />
        <Route
          path="hour-types"
          element={
            <RequireEmployer>
              <HourTypesPage />
            </RequireEmployer>
          }
        />
        <Route
          path="projects"
          element={
            <RequireEmployer>
              <ProjectsPage />
            </RequireEmployer>
          }
        />
        <Route
          path="email"
          element={
            <RequireEmployer>
              <EmailSettingsPage />
            </RequireEmployer>
          }
        />
        <Route
          path="tenants"
          element={
            <RequirePlatformAdmin>
              <TenantsPage />
            </RequirePlatformAdmin>
          }
        />
        <Route
          path="platform-admins"
          element={
            <RequirePlatformAdmin>
              <PlatformAdminsPage />
            </RequirePlatformAdmin>
          }
        />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
