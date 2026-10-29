import { Navigate, Route, Routes } from "react-router-dom";
import { useAuth } from "./auth/AuthContext";
import { AppShell } from "./components/AppShell";
import { LoginPage } from "./pages/LoginPage";
import { ForgotPasswordPage } from "./pages/ForgotPasswordPage";
import { ChoosePasswordPage } from "./pages/ChoosePasswordPage";
import { DashboardPage } from "./pages/DashboardPage";
import { ReportsPage } from "./pages/ReportsPage";
import { TeamPage } from "./pages/TeamPage";
import { EmployeesPage } from "./pages/EmployeesPage";
import { AccountPage } from "./pages/AccountPage";
import { HourTypesPage } from "./pages/HourTypesPage";
import { ProjectsPage } from "./pages/ProjectsPage";
import { TenantsPage } from "./pages/TenantsPage";
import { PlatformAdminsPage } from "./pages/PlatformAdminsPage";
import { LeavePage } from "./pages/LeavePage";
import { CompanyPage } from "./pages/CompanyPage";

function RequireAuth({ children }: { children: JSX.Element }) {
  const { isAuthenticated, user } = useAuth();
  if (!isAuthenticated) return <Navigate to="/login" replace />;
  // A password someone else set must be replaced before anything else (the API enforces this too).
  return user?.mustChangePassword ? <ChoosePasswordPage /> : children;
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
        path="/forgot-password"
        element={isAuthenticated ? <Navigate to="/" replace /> : <ForgotPasswordPage />}
      />
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
          path="company"
          element={
            <RequireEmployer>
              <CompanyPage />
            </RequireEmployer>
          }
        />
        {/* Email settings moved to the Company page. */}
        <Route path="email" element={<Navigate to="/company" replace />} />
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
