import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { LoginPage } from "../features/auth/pages/LoginPage";
import { RegisterPage } from "../features/auth/pages/RegisterPage";
import { WelcomePage } from "../features/auth/pages/WelcomePage";
import { DashboardPage } from "../features/dashboard/pages/DashboardPage";
import { ClientsPage } from "../features/clients/pages/ClientsPage";
import { CompanySettingsPage } from "../features/company-settings/pages/CompanySettingsPage";
import { AdminUsersPage } from "../features/admin/pages/AdminUsersPage";
import {
  GuestOnly,
  HomeRedirect,
  RequireAdmin,
  RequireAuth,
  RequireVisitor,
} from "../shared/auth/route-guards";

export function AppRouter() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<HomeRedirect />} />

        <Route element={<GuestOnly />}>
          <Route path="/welcome" element={<WelcomePage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
        </Route>

        <Route element={<RequireAuth />}>
          <Route element={<RequireVisitor />}>
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/settings/company" element={<CompanySettingsPage />} />
            <Route path="/clients" element={<ClientsPage />} />
            <Route path="/items" element={<div className="card">{/* Items module */}Items module</div>} />
            <Route path="/invoices" element={<div className="card">Invoices module</div>} />
            <Route path="/reports" element={<div className="card">Reports module</div>} />
          </Route>

          <Route element={<RequireAdmin />}>
            <Route path="/admin/users" element={<AdminUsersPage />} />
          </Route>
        </Route>

        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
