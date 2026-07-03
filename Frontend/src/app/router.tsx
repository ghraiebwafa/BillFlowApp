import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { LoginPage } from "../features/auth/pages/LoginPage";
import { RegisterPage } from "../features/auth/pages/RegisterPage";
import { WelcomePage } from "../features/auth/pages/WelcomePage";
import { ForgotPasswordPage } from "../features/auth/pages/ForgotPasswordPage";
import { DashboardPage } from "../features/dashboard/pages/DashboardPage";
import { ClientsPage } from "../features/clients/pages/ClientsPage";
import { CompanySettingsPage } from "../features/company-settings/pages/CompanySettingsPage";
import { AdminUsersPage } from "../features/admin/pages/AdminUsersPage";
import { ProfilePage } from "../features/profile/pages/ProfilePage";
import { InvoicesPage } from "../features/invoices/pages/InvoicesPage";
import { InvoiceDetailPage } from "../features/invoices/pages/InvoiceDetailPage";
import { CreateInvoicePage } from "../features/invoices/pages/CreateInvoicePage";
import { PaymentsPage } from "../features/payments/pages/PaymentsPage";
import { ReportsPage } from "../features/reports/pages/ReportsPage";
import { ActivityPage } from "../features/activity/pages/ActivityPage";
import { ComingSoonPage } from "../shared/ui/ComingSoonPage";
import { PortalPage } from "../features/portal/pages/PortalPage";
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
          <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        </Route>

        <Route element={<RequireAuth />}>
          <Route element={<RequireVisitor />}>
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/profile" element={<ProfilePage />} />
            <Route path="/settings/company" element={<CompanySettingsPage />} />
            <Route path="/clients" element={<ClientsPage />} />
            <Route path="/invoices" element={<InvoicesPage />} />
            <Route path="/invoices/new" element={<CreateInvoicePage />} />
            <Route path="/invoices/:id" element={<InvoiceDetailPage />} />
            <Route path="/payments" element={<PaymentsPage />} />
            <Route path="/reports" element={<ReportsPage />} />
            <Route path="/activity" element={<ActivityPage />} />
            <Route
              path="/items"
              element={<ComingSoonPage titleKey="nav.items" subtitleKey="dashboard.modules.items.desc" />}
            />
          </Route>

          <Route element={<RequireAdmin />}>
            <Route path="/admin/users" element={<AdminUsersPage />} />
          </Route>
        </Route>

        <Route path="/portal/:token" element={<PortalPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
