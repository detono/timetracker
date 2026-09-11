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

function RequireAuth({ children }: { children: JSX.Element }) {
  const { isAuthenticated } = useAuth();
  return isAuthenticated ? children : <Navigate to="/login" replace />;
}

function RequireEmployer({ children }: { children: JSX.Element }) {
  const { user } = useAuth();
  return user?.role === "Employer" ? children : <Navigate to="/" replace />;
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
        <Route index element={<DashboardPage />} />
        <Route path="reports" element={<ReportsPage />} />
        <Route path="team" element={<TeamPage />} />
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
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
